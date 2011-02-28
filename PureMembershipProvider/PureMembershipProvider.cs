using System;
using System.Text;
using System.Web.Security;
using System.Configuration.Provider;
using System.Collections.Specialized;
using System.Configuration;
using System.Security.Cryptography;
using MySql.Data.MySqlClient;

namespace PureDev.Common
{
    public sealed class PureMembershipProvider : MembershipProvider, IDisposable
    {
        #region Queries

        private const string SQL_CreateUser = @"
INSERT INTO `users`
(`userName`,
`password`,
`email`,
`creationDate`,
`approved`,
`lastPasswordChangedDate`,
`lastLoginDate`,
`lastActivityDate`,
`locked`,
`lastLockedDate`,
`failedPswAttemptCount`,
`failedPswAttemptsStart`)
VALUES ( ?userName, ?password, ?email, NOW(), ?approved, NULL, NULL, NULL, 0, NULL, NULL, NULL );";


        #endregion

        #region Fields

        private const int newPasswordLength = 8;
        private MembershipHelper _helper;

        private bool pEnablePasswordReset;
        private bool pRequiresUniqueEmail;
        private int pMaxInvalidPasswordAttempts;
        private int pPasswordAttemptWindow;
        private byte[] SecretHashKey;

        public PureMembershipProvider()
        {
            pRequiresUniqueEmail = true;
            ConnectionStringSettings ConnectionStringSettings = ConfigurationManager.ConnectionStrings["defaultConnectionString"];
            if (ConnectionStringSettings == null || ConnectionStringSettings.ConnectionString.Trim() == "")
            {
                throw new ProviderException("Connection string cannot be blank.");
            }

            _helper = new MembershipHelper(ConnectionStringSettings.ConnectionString);
        }

        #endregion

        //
        // System.Configuration.Provider.ProviderBase.Initialize Method
        //

        public override void Initialize(string name, NameValueCollection config)
        {
            //
            // Initialize values from web.config.
            //

            if (config == null)
                throw new ArgumentNullException("config");

            if (name.Length == 0)
                name = "OdbcMembershipProvider";

            // Initialize the abstract base class.
            base.Initialize(name, config);
            
            pMaxInvalidPasswordAttempts = Convert.ToInt32(GetConfigValue(config["maxInvalidPasswordAttempts"], "5"));
            pPasswordAttemptWindow = Convert.ToInt32(GetConfigValue(config["passwordAttemptWindow"], "10"));
            pMinRequiredNonAlphanumericCharacters = Convert.ToInt32(GetConfigValue(config["minRequiredNonAlphanumericCharacters"], "1"));
            pMinRequiredPasswordLength = Convert.ToInt32(GetConfigValue(config["minRequiredPasswordLength"], "7"));
            pPasswordStrengthRegularExpression = Convert.ToString(GetConfigValue(config["passwordStrengthRegularExpression"], ""));
            pEnablePasswordReset = Convert.ToBoolean(GetConfigValue(config["enablePasswordReset"], "true"));
            pRequiresUniqueEmail = Convert.ToBoolean(GetConfigValue(config["requiresUniqueEmail"], "true"));
            SecretHashKey = Encoding.Unicode.GetBytes(ConfigurationManager.AppSettings["SecretHashKey"]);

            string temp_format = config["passwordFormat"] ?? "Hashed";
            if(temp_format != "Hashed")
                throw new ProviderException("Password format not supported. Only hash with special salt is supported! Choose 'Hashed'.");
            
            //
            // Initialize OdbcConnection.
            //

            ConnectionStringSettings ConnectionStringSettings = ConfigurationManager.ConnectionStrings[config["connectionStringName"]];

            if (ConnectionStringSettings == null || ConnectionStringSettings.ConnectionString.Trim() == "")
            {
                throw new ProviderException("Connection string cannot be blank.");
            }

            _helper = new MembershipHelper(ConnectionStringSettings.ConnectionString);
        }

        //
        // System.Web.Security.MembershipProvider properties.
        //

        #region Properties

        [Obsolete]
        public override string ApplicationName { get; set; }

        public override bool EnablePasswordReset
        {
            get { return pEnablePasswordReset; }
        }

        [Obsolete]
        public override bool EnablePasswordRetrieval
        {
            get { return false; }
        }


        public override bool RequiresUniqueEmail
        {
            get { return pRequiresUniqueEmail; }
        }

        [Obsolete]
        public override bool RequiresQuestionAndAnswer
        {
            get { return false; }
        }

        public override int MaxInvalidPasswordAttempts
        {
            get { return pMaxInvalidPasswordAttempts; }
        }


        public override int PasswordAttemptWindow
        {
            get { return pPasswordAttemptWindow; }
        }


        public override MembershipPasswordFormat PasswordFormat
        {
            get { return MembershipPasswordFormat.Hashed; }
        }

        private int pMinRequiredNonAlphanumericCharacters;

        public override int MinRequiredNonAlphanumericCharacters
        {
            get { return pMinRequiredNonAlphanumericCharacters; }
        }

        private int pMinRequiredPasswordLength;

        public override int MinRequiredPasswordLength
        {
            get { return pMinRequiredPasswordLength; }
        }

        private string pPasswordStrengthRegularExpression;

        public override string PasswordStrengthRegularExpression
        {
            get { return pPasswordStrengthRegularExpression; }
        }

        #endregion

        public override bool ChangePassword(string username, string oldPwd, string newPwd)
        {
            if (!ValidateUser(username, oldPwd))
                return false;

            var args = new ValidatePasswordEventArgs(username, newPwd, true);

            OnValidatingPassword(args);

            if (args.Cancel)
                if (args.FailureInformation != null)
                    throw args.FailureInformation;
                else
                    throw new MembershipPasswordException("Change password canceled due to new password validation failure.");

            var parameters = new[] {
                        new MySqlParameter("?psw", EncodePassword(newPwd)),
                        new MySqlParameter("?username", username)
            };

            return _helper.ExecuteOdbcQuery("UPDATE `Users` SET `password` = ?psw, `lastPasswordChangedDate` = NOW() WHERE `userName` = ?username", parameters) > 0;
        }

        /// <summary>
        /// Not supported due to security reasons
        /// </summary>
        [Obsolete]
        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPwdQuestion, string newPwdAnswer)
        {
            throw new NotImplementedException("This functionality is not supported!");
        }

        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            var args = new ValidatePasswordEventArgs(username, password, true);

            OnValidatingPassword(args);

            if (args.Cancel)
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }

            if (RequiresUniqueEmail && GetUserNameByEmail(email) != "")
            {
                status = MembershipCreateStatus.DuplicateEmail;
                return null;
            }

            var u = GetUser(username, false);
            if (u == null)
            {
                var parameters = new[] {
                                         new MySqlParameter("?userName", username),
                                         new MySqlParameter("?password", EncodePassword(password)),
                                         new MySqlParameter("?email", email),
                                         new MySqlParameter("?approved", isApproved)
                                     };
                int recAdded = _helper.ExecuteOdbcQuery(SQL_CreateUser, parameters);
                status = recAdded > 0 ? MembershipCreateStatus.Success : MembershipCreateStatus.UserRejected;

                return GetUser(username, false);
            }
            status = MembershipCreateStatus.DuplicateUserName;
            return null;
        }

        public MembershipUser CreateUser(string username, string password, string email, bool isApproved)
        {
            MembershipCreateStatus status;
            return CreateUser(username, password, email, null, null, isApproved, null, out status);
        }

        //
        // MembershipProvider.DeleteUser
        //

        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            var parameters = new[] { new MySqlParameter("?userName", username) };

            int rowsAffected = _helper.ExecuteOdbcQuery("DELETE FROM `Users` WHERE `userName` = ?userName;", parameters);

            if(deleteAllRelatedData )
            {
                
            }

            return rowsAffected > 0;
        }



        //
        // MembershipProvider.GetAllUsers
        //

        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            var parameters = new[] { 
                new MySqlParameter("?offset", pageSize * pageIndex), 
                new MySqlParameter("?count", pageSize) };
            var collection = new MembershipUserCollection();
            int totalUsers;
            Action<MySqlDataReader> cnt = reader =>
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                        collection.Add(GetUserFromReader(reader));
                }
            };
            _helper.ExecuteMySqlReader3("SELECT SQL_CALC_FOUND_ROWS * FROM `Users` ORDER BY `userName` LIMIT ?offset, ?count;", parameters, cnt, out totalUsers);
            totalRecords = totalUsers;
            return collection;
        }


        //
        // MembershipProvider.GetNumberOfUsersOnline
        //

        public override int GetNumberOfUsersOnline()
        {

            var onlineSpan = new TimeSpan(0, Membership.UserIsOnlineTimeWindow, 0);
            DateTime compareTime = DateTime.Now.Subtract(onlineSpan);

            var parameters = new[] { new MySqlParameter("?lActiveDate", compareTime) };
            var result = _helper.ExecuteOdbcScalar("SELECT COUNT(*) FROM `Users` WHERE `lastActivityDate` > ?lActiveDate;", parameters);
            return result != DBNull.Value ? Convert.ToInt32(result) : 0;
        }

        /// <summary>
        /// Not supported due to security reasons
        /// </summary>
        [Obsolete]
        public override string GetPassword(string username, string answer)
        {
            throw new ProviderException("Password Retrieval is not possible!");
        }

        //
        // MembershipProvider.GetUser(string, bool)
        //

        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            var parameters = new[] { new MySqlParameter("?userName", username) };
            MembershipUser u = null;

            Action<MySqlDataReader> cnt = reader =>
            {
                if (reader.HasRows)
                {
                    reader.Read();
                    u = GetUserFromReader(reader);
                    if (userIsOnline)
                        _helper.ExecuteOdbcQuery("UPDATE `Users` SET `lastActivityDate` = NOW() WHERE `userName` = ?userName;",
                                         parameters);
                }
            };
            _helper.ExecuteMySqlReader2("SELECT * FROM `Users` WHERE `userName` = ?userName;", parameters, cnt);
            
            return u;
        }


        //
        // MembershipProvider.GetUser(object, bool)
        //

        public override MembershipUser GetUser(object id_user, bool userIsOnline)
        {
            var parameters = new[] { new MySqlParameter("?id_user", id_user) };
            MembershipUser u = null;

            Action<MySqlDataReader> cnt = reader =>
            {
                if (reader.HasRows)
                {
                    reader.Read();
                    u = GetUserFromReader(reader);
                    if (userIsOnline)
                        _helper.ExecuteOdbcQuery("UPDATE `Users` SET `lastActivityDate` = NOW() WHERE `id_user` = ?id_user;",
                                         parameters);
                }
                else
                    throw new Exception("User with id_user = '" + id_user + "' doesn't exists!");
            };
            _helper.ExecuteMySqlReader2("SELECT * FROM `Users` WHERE `id_user` = ?id_user;", parameters, cnt);

            return u;
        }

        //
        // MembershipProvider.UnlockUser
        //

        public override bool UnlockUser(string username)
        {
            var parameters = new[] { new MySqlParameter("?username", username) };

            return _helper.ExecuteOdbcQuery("UPDATE `Users` SET `lastLockedDate` = NOW(), `locked` = '0', `failedPswAttemptCount` = '0' WHERE `userName` = ?userName;",
                                                     parameters) > 0;
        }


        //
        // MembershipProvider.GetUserNameByEmail
        //

        public override string GetUserNameByEmail(string email)
        {
            var param = new MySqlParameter("?email", email);
            var username = (string)_helper.ExecuteOdbcScalar("SELECT Username FROM Users WHERE Email = ?email", new[] { param });
            return username ?? "";
        }

        //
        // MembershipProvider.ResetPassword
        //

        public override string ResetPassword(string username, string answer)
        {
            if (!EnablePasswordReset)
            {
                throw new NotSupportedException("Password reset is not enabled.");
            }

            string newPassword = Membership.GeneratePassword(newPasswordLength, MinRequiredNonAlphanumericCharacters);

            var args = new ValidatePasswordEventArgs(username, newPassword, true);

            OnValidatingPassword(args);

            if (args.Cancel)
                if (args.FailureInformation != null)
                    throw args.FailureInformation;
                else
                    throw new MembershipPasswordException("Reset password canceled due to password validation failure.");

            var parameters = new[] { new MySqlParameter("?userName", username) };
            Action<MySqlDataReader> cnt = reader =>
            {
                if (reader.HasRows && reader.Read())
                {
                    if (reader.GetBoolean("locked"))
                        throw new MembershipPasswordException("The supplied user is locked out.");
                }
                else
                    throw new MembershipPasswordException("The supplied user name is not found.");
            };
            _helper.ExecuteMySqlReader2("SELECT `locked` FROM `Users` WHERE `userName` = ?userName;", parameters, cnt);

            parameters = new[] { new MySqlParameter("?userName", username), new MySqlParameter("?psw", EncodePassword(newPassword)) };
            int rowsAffected = _helper.ExecuteOdbcQuery("UPDATE `Users` SET `password` = ?psw, `lastPasswordChangedDate` = NOW() WHERE `userName` = ?userName;", parameters); 

            if (rowsAffected > 0)
            {
                return newPassword;
            }
            throw new MembershipPasswordException("User not found, or user is locked out. Password not Reset.");
        }


        //
        // MembershipProvider.UpdateUser
        //

        public override void UpdateUser(MembershipUser user)
        {
            var parameters = new[] {
                        new MySqlParameter("?email", user.Email),
                        new MySqlParameter("?approved", user.IsApproved),
                        new MySqlParameter("?username", user.UserName)
            };

            _helper.ExecuteOdbcQuery("UPDATE Users SET `email` = ?email, `approved` = ?approved WHERE `userName` = ?username; ", parameters);
        }


        //
        // MembershipProvider.ValidateUser
        //

        public override bool ValidateUser(string username, string password)
        {
            bool isValid = false;
            bool isApproved = false;
            string pwd = "";
            var parameters = new[] { new MySqlParameter("?userName", username) };

            Action<MySqlDataReader> cnt = reader =>
            {
                if (reader.HasRows && reader.Read())
                {
                    pwd = reader.GetString(0);
                    isApproved = reader.GetBoolean(1);
                }
            };
            _helper.ExecuteMySqlReader2("SELECT `password`, `approved` FROM `Users` WHERE `userName` = ?userName;", parameters, cnt);
            if (CheckPassword(password, pwd))
            {
                if (isApproved)
                {
                    isValid = true;
                    _helper.ExecuteOdbcQuery("UPDATE `Users` SET `lastLoginDate` = NOW() WHERE `userName` = ?userName;", parameters);
                }
            }
            else 
                UpdateFailureCount(username, "password");

            return isValid;
        }


        //
        // MembershipProvider.FindUsersByName
        //

        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            var parameters = new[] { 
                new MySqlParameter("?valueToMatch", "%" + usernameToMatch + "%"), 
                new MySqlParameter("?offset", pageSize * pageIndex), 
                new MySqlParameter("?count", pageSize) };
            var collection = new MembershipUserCollection();
            int totalUsers;
            Action<MySqlDataReader> cnt = reader =>
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                        collection.Add(GetUserFromReader(reader));
                }
            };
            _helper.ExecuteMySqlReader3("SELECT SQL_CALC_FOUND_ROWS * FROM `Users` WHERE `userName` LIKE ?valueToMatch LIMIT ?offset, ?count;", parameters, cnt, out totalUsers);
            totalRecords = totalUsers;
            return collection;
        }

        //
        // MembershipProvider.FindUsersByEmail
        //

        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            var parameters = new[] { 
                new MySqlParameter("?emailToMatch", "%" + emailToMatch + "%"), 
                new MySqlParameter("?offset", pageSize * pageIndex), 
                new MySqlParameter("?count", pageSize) };
            var collection = new MembershipUserCollection() ;
            int totalUsers;
            Action<MySqlDataReader> cnt = reader =>
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                        collection.Add(GetUserFromReader(reader));
                }
            };
            _helper.ExecuteMySqlReader3("SELECT SQL_CALC_FOUND_ROWS * FROM `Users` WHERE `email` LIKE ?emailToMatch LIMIT ?offset, ?count;", parameters, cnt, out totalUsers);
            totalRecords = totalUsers;
            return collection;
        }

        #region Helpers


        private void UpdateFailureCount(string username, string failureType)
        {
            var parameters = new[] { new MySqlParameter("?userName", username) };
            int failedPswAttemptCount = 0;
            DateTime failedPswAttemptsStart = new DateTime();
            Action<MySqlDataReader> cnt = reader =>
            {
                if (reader.HasRows && reader.Read())
                {
                    failedPswAttemptCount = reader["failedPswAttemptCount"] != DBNull.Value ? reader.GetInt32("failedPswAttemptCount") : 0;
                    if (reader["failedPswAttemptsStart"] != DBNull.Value)
                        failedPswAttemptsStart = reader.GetDateTime("failedPswAttemptsStart");
                }
            };
            _helper.ExecuteMySqlReader2("SELECT `failedPswAttemptCount`, `failedPswAttemptsStart` FROM `Users` WHERE `userName` = ?userName;", parameters, cnt);

            DateTime windowEnd = failedPswAttemptsStart.AddMinutes(PasswordAttemptWindow);
            if (failedPswAttemptCount == 0 || DateTime.Now > windowEnd)
            {
                // First password failure or outside of PasswordAttemptWindow.
                // Start a new password failure count from 1 and a new window starting now.
                if (failureType == "password")
                    if (_helper.ExecuteOdbcQuery("UPDATE `Users` SET `failedPswAttemptCount` = '1', `failedPswAttemptsStart` = NOW() WHERE `userName` = ?userName;", parameters) < 0)
                        throw new ProviderException("Unable to update failure count and window start.");
            }
            else
            {
                if (failedPswAttemptCount++ >= MaxInvalidPasswordAttempts)
                {
                    // Password attempts have exceeded the failure threshold. Lock out the user.
                    if (_helper.ExecuteOdbcQuery("UPDATE `Users` SET `locked` = '1', `lastLockedDate` = NOW() WHERE `userName` = ?userName;", parameters) < 0)
                        throw new ProviderException("Unable to lock out user.");
                }
                else
                {
                    // Password attempts have not exceeded the failure threshold. Update
                    // the failure counts. Leave the window the same.
                    var localParams = new[] { new MySqlParameter("?userName", username), new MySqlParameter("?fCount", failedPswAttemptCount) };

                    if (failureType == "password")
                        if (_helper.ExecuteOdbcQuery("UPDATE `Users` SET `failedPswAttemptCount` = ?fCount WHERE `userName` = ?userName;", localParams) < 0)
                            throw new ProviderException("Unable to update failure count.");
                }
            }
        }


        private static MembershipUser GetUserFromReader(MySqlDataReader reader)
        {
            int id_user = reader.GetInt32("id_user");
            string username = reader.GetString("userName");
            string email = reader.GetString("email");
            bool isApproved = reader.GetBoolean("approved");
            bool isLockedOut = reader.GetBoolean("locked");
            DateTime createdDate = reader.GetDateTime("creationDate");

            DateTime lastLoginDate = reader["lastLoginDate"] != DBNull.Value ? reader.GetDateTime("lastLoginDate") : DateTime.MinValue;
            DateTime lastActivityDate = reader["lastActivityDate"] != DBNull.Value ? reader.GetDateTime("lastActivityDate") : DateTime.MinValue;
            DateTime lastLockedDate = reader["lastLockedDate"] != DBNull.Value ? reader.GetDateTime("lastLockedDate") : DateTime.MinValue;
            DateTime lastPasswordChangedDate = reader["lastPasswordChangedDate"] != DBNull.Value ? reader.GetDateTime("lastPasswordChangedDate") : DateTime.MinValue;

            return new MembershipUser("PureMembershipProvider", username, null, email, null, null, isApproved,
                                      isLockedOut, createdDate, lastLoginDate, lastActivityDate, lastPasswordChangedDate,
                                      lastLockedDate);
            //return new PureMembershipUser(id_user, username, email, isApproved, isLockedOut,
            //    createdDate, lastLoginDate, lastActivityDate, lastPasswordChangedDate, lastLockedDate);

        }

        private bool CheckPassword(string password, string dbpassword)
        {
            return EncodePassword(password) == dbpassword;
        }

        private string EncodePassword(string password)
        {
            var hash = new HMACSHA1 { Key = SecretHashKey };
            return Convert.ToBase64String(hash.ComputeHash(Encoding.Unicode.GetBytes(password)));
        }


        private static string GetConfigValue(string configValue, string defaultValue)
        {
            return String.IsNullOrEmpty(configValue) ? defaultValue : configValue;
        }


        #endregion


        #region IDisposable Members

        public void Dispose()
        {
            _helper.Dispose();
        }

        #endregion
    }
}

