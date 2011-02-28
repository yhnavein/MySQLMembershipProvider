using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Configuration.Provider;
using System.Linq;
using System.Web.Security;
using MySql.Data.MySqlClient;

namespace PureDev.Common
{
  public sealed class PureRoleProvider : RoleProvider, IDisposable
    {
        private MembershipHelper _helper;

        private ConnectionStringSettings pConnectionStringSettings;
        private string connectionString;

        private static List<string> _rolesTemp;


        public override void Initialize(string name, NameValueCollection config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            if (name.Length == 0)
                name = "OdbcRoleProvider";

            if (String.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", "Sample ODBC Role provider");
            }

            // Initialize the abstract base class.
            base.Initialize(name, config);

            pConnectionStringSettings = ConfigurationManager.
              ConnectionStrings[config["connectionStringName"]];

            if (pConnectionStringSettings == null || pConnectionStringSettings.ConnectionString.Trim() == "")
            {
                throw new ProviderException("Connection string cannot be blank.");
            }

            connectionString = pConnectionStringSettings.ConnectionString;
            _helper = new MembershipHelper(connectionString);
        }

        [Obsolete]
        public override string ApplicationName{get;set;}

        //
        // RoleProvider.AddUsersToRoles
        //

        public override void AddUsersToRoles(string[] usernames, string[] rolenames)
        {
            if (rolenames.Any(rolename => !RoleExists(rolename)))
            {
                throw new ProviderException("Role name not found.");
            }

            foreach (string username in usernames)
            {
                if (username.Contains(","))
                {
                    throw new ArgumentException("User names cannot contain commas.");
                }

                foreach (string r in rolenames)
                {
                    if (IsUserInRole(username, r))
                    {
                        throw new ProviderException("User is already in role.");
                    }
                }
            }
            var parameters = new[] {
                                     new MySqlParameter("?roleName", MySqlDbType.VarChar), 
                                     new MySqlParameter("?userName", MySqlDbType.VarChar)
                                 };

            string query = @"INSERT INTO `UserRoles` (`id_user`, `id_role`) VALUES 
((SELECT `id_user` FROM `Users` WHERE `userName` = ?userName LIMIT 1),
(SELECT `id_role` FROM `Roles` WHERE `name` = ?roleName  LIMIT 1));";

            //tak jest wydajniej o jakies 3,000 razy xD
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    using (var cmd = new MySqlCommand(query, conn, trans))
                    {
                        cmd.Parameters.AddRange(parameters);

                        foreach (string username in usernames)
                        {
                            foreach (string rolename in rolenames)
                            {
                                cmd.Parameters[0].Value = rolename;
                                cmd.Parameters[1].Value = username;
                                if (cmd.ExecuteNonQuery() == 0)
                                    throw new ProviderException("Something has happened! User `" + username + "` failed in assignin to `" + rolename + "`!");
                            }
                        }
                        trans.Commit();
                    }
                }
            }
        }


        //
        // RoleProvider.CreateRole
        //
        public void CreateRole1(string rolename)
        {
            if (rolename.Contains(","))
                throw new ArgumentException("Role names cannot contain commas.");

            if (RoleExists(rolename))
            {
                throw new ProviderException("Role name already exists.");
            }

            var parameters = new[] { new MySqlParameter("?name", rolename) };
            _helper.ExecuteOdbcQuery("INSERT INTO `Roles` (`name`) VALUES ( ?name ); ", parameters);
        }

        public override void CreateRole(string rolename)
        {
            DateTime now = DateTime.Now;
            if (rolename.Contains(","))
                throw new ArgumentException("Role names cannot contain commas.");

            if (RoleExists(rolename))
            {
                throw new ProviderException("Role name already exists.");
            }

            _helper.ExecuteOdbcQuery("CALL `create_role` ('" + MySqlHelper.DoubleQuoteString(rolename) + "');");
            Console.WriteLine("Role {0} added in {1}", rolename, (DateTime.Now - now));
        }


        //
        // RoleProvider.DeleteRole
        //

        public override bool DeleteRole(string rolename, bool throwOnPopulatedRole)
        {
            if (!RoleExists(rolename))
            {
                throw new ProviderException("Role does not exist.");
            }

            if (throwOnPopulatedRole && GetUsersInRole(rolename).Length > 0)
            {
                throw new ProviderException("Cannot delete a populated role.");
            }
            var parameters = new[] { new MySqlParameter("?roleName", rolename) };
            string query1 = @"DELETE UR.* FROM `UserRoles` UR JOIN `Roles` R ON (R.`id_role` = UR.`id_role`)  WHERE R.`name` = ?roleName;";

            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    using (var cmd = new MySqlCommand(query1, conn, trans))
                    {
                        cmd.Parameters.AddRange(parameters);
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "DELETE FROM `Roles` WHERE `name` = ?roleName; ";
                        cmd.ExecuteNonQuery();
                        trans.Commit();
                        return true;
                    }
                }
            }
        }


        //
        // RoleProvider.GetAllRoles
        //

        public override string[] GetAllRoles()
        {
            var parameters = new MySqlParameter[0];
            var collection = new List<string>(32);
            int totalRecords;
            Action<MySqlDataReader> cnt = reader =>
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                        collection.Add(reader.GetString("name"));
                }
            };
            _helper.ExecuteMySqlReader3("SELECT SQL_CALC_FOUND_ROWS R.`name` FROM `Roles` R;", parameters, cnt, out totalRecords);

            return collection.ToArray();
        }


        //
        // RoleProvider.GetRolesForUser
        //

        public override string[] GetRolesForUser(string username)
        {
            var parameters = new[] { new MySqlParameter("?userName", username) };
            var collection = new List<string>(16);
            int totalRecords;
            Action<MySqlDataReader> cnt = reader =>
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                        collection.Add(reader.GetString("name"));
                }
            };
            _helper.ExecuteMySqlReader3("SELECT SQL_CALC_FOUND_ROWS R.`name` FROM `Roles` R JOIN `UserRoles` UR ON (R.`id_role` = UR.`id_role`)  JOIN `Users` U ON (U.`id_user` = UR.`id_user`) WHERE U.`userName` = ?userName;", parameters, cnt, out totalRecords);

            return collection.ToArray();
        }


        //
        // RoleProvider.GetUsersInRole
        //

        public override string[] GetUsersInRole(string rolename)
        {
            var parameters = new[] { new MySqlParameter("?roleName", rolename) };
            var collection = new List<string>(32);
            int totalRecords;
            Action<MySqlDataReader> cnt = reader =>
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                        collection.Add(reader.GetString("userName"));
                }
            };
            _helper.ExecuteMySqlReader3("SELECT SQL_CALC_FOUND_ROWS `userName` FROM `Users` U JOIN `UserRoles` UR ON (U.`id_user` = UR.`id_user`) JOIN `Roles` R ON (R.`id_role` = UR.`id_role`) WHERE R.`name` = ?roleName;", parameters, cnt, out totalRecords);

            return collection.ToArray();
        }

        public override bool IsUserInRole(string username, string rolename)
        {
            var parameters = new[] { new MySqlParameter("?rolename", rolename), new MySqlParameter("?username", username) };
            var obj = _helper.ExecuteOdbcScalar("SELECT COUNT(*) FROM `Users` U JOIN `UserRoles` UR ON (U.`id_user` = UR.`id_user`) JOIN `Roles` R ON (R.`id_role` = UR.`id_role`) WHERE R.`name` = ?rolename AND U.`userName` = ?username; ", parameters);
            return obj != DBNull.Value ? Convert.ToInt32(obj) > 0 : false;
        }


        //
        // RoleProvider.RemoveUsersFromRoles
        //

        public override void RemoveUsersFromRoles(string[] usernames, string[] rolenames)
        {
            if (rolenames.Any(rolename => !RoleExists(rolename)))
            {
                throw new ProviderException("Role name not found.");
            }

            if ((from username in usernames
                 from rolename in rolenames
                 where !IsUserInRole(username, rolename)
                 select username).Any())
            {
                throw new ProviderException("User is not in role.");
            }

            for (int i = 0; i < rolenames.Length;i++ )
                rolenames[i] = MySqlHelper.EscapeString(rolenames[i]);
            for (int i = 0; i < usernames.Length; i++)
                usernames[i] = MySqlHelper.EscapeString(usernames[i]);

           var userNames = "'" + string.Join("', '", usernames) + "'";
           var roleNames = "'" + string.Join("', '", rolenames) + "'";
           string query = @"DELETE UR.* FROM `UserRoles` UR JOIN `Users` U ON (U.`id_user` = UR.`id_user`) 
JOIN `Roles` R ON (R.`id_role` = UR.`id_role`)  WHERE U.`userName` IN (" + userNames + ") AND R.`name` IN (" + roleNames + ");";

           _helper.ExecuteOdbcQuery(query);
        }


        //
        // RoleProvider.RoleExists
        //

        public bool RoleExists1(string rolename)
        {
            var parameters = new[] { new MySqlParameter("?name", rolename) };
            var obj = _helper.ExecuteOdbcScalar("SELECT COUNT(*) FROM `Roles` WHERE `name` = ?name; ", parameters);
            return obj != DBNull.Value ? Convert.ToInt32(obj) > 0 : false;
        }

        public override bool RoleExists(string rolename)
        {
            var obj = _helper.ExecuteOdbcScalar("CALL `role_exists` ('" + rolename + "'); ");
            return obj != DBNull.Value ? Convert.ToInt32(obj) > 0 : false;
        }

        //
        // RoleProvider.FindUsersInRole
        //

        public override string[] FindUsersInRole(string rolename, string usernameToMatch)
        {
            var parameters = new[] { 
                new MySqlParameter("?userName", "%" + usernameToMatch + "%"), 
                new MySqlParameter("?roleName", rolename) };
            var collection = new List<string>(16);
            int totalRecords;
            Action<MySqlDataReader> cnt = reader =>
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                        collection.Add(reader.GetString("userName"));
                }
            };
            _helper.ExecuteMySqlReader3("SELECT SQL_CALC_FOUND_ROWS `userName` FROM `Users` U JOIN `UserRoles` UR ON (U.`id_user` = UR.`id_user`) JOIN `Roles` R ON (R.`id_role` = UR.`id_role`) WHERE R.`name` = ?roleName AND U.`userName` LIKE ?userName;", parameters, cnt, out totalRecords);

            return collection.ToArray();
        }

      public void Dispose()
      {
          _helper.Dispose();
          
      }
    }
}
