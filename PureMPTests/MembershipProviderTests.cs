using System;
using System.Web.Security;
using NUnit.Framework;

namespace PureDev.Common
{
    [TestFixture]
    public class MembershipProviderTests
    {
        PureMembershipProvider mp;
        SqlMembershipProvider smp;
        const string domain = "puredev.eu";
        const string specialUN = "malinek";
        const string specialPSW = "malinkapralinka";

        [TestFixtureSetUp]
        public void Init()
        {
            try
            {
                mp = (PureMembershipProvider)Membership.Providers["PureMembershipProvider"];
                smp = (SqlMembershipProvider)Membership.Providers["CustomSqlMembershipProvider"];
                CreateSpecialUser();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        [Test]
        public void CreateSpecialUser()
        {
            string user = specialUN;
            MembershipCreateStatus status;
            var muser = mp.CreateUser(user, specialPSW, user + "@" + domain, null, null, true, null, out status);
            if (muser == null)
            {
                muser = mp.GetUser(user, false);
            }

            Assert.IsNotNull(muser);
            Assert.AreEqual(muser.Email, user + "@" + domain);
            Assert.AreEqual(muser.UserName, user);
        }

        [Test]
        public void TestCreatingUsers()
        {
            string user = Guid.NewGuid().ToString().Substring(0, 12);
            MembershipCreateStatus status;
            var muser = mp.CreateUser(user, "lalagugu1", user + "@" + domain, null, null, true, null, out status);
            Assert.IsNotNull(muser);
            Assert.AreEqual(muser.Email, user + "@" + domain);
            Assert.AreEqual(muser.UserName, user);
            Assert.IsTrue(mp.DeleteUser(user, true));
            Assert.IsNull(mp.GetUser(user, false));
        }

        [Test]
        public void TestLoginSucc()
        {
            var logged = mp.ValidateUser(specialUN, specialPSW);
            Assert.IsTrue(logged);

            var user = mp.GetUser(specialUN, false);
            Assert.IsNotNull(user);
            Assert.AreEqual(user.LastLoginDate.Date, DateTime.Today, "Last login date is wrong!");
            Assert.AreEqual(user.LastLoginDate.Hour, DateTime.Now.Hour, "Last login hour is wrong!");
            Assert.AreEqual(user.LastLoginDate.Minute, DateTime.Now.Minute, "Last login Minute is wrong!");
            Assert.AreEqual(user.LastLoginDate.Second, DateTime.Now.Second, "Last login Second is wrong!");
        }

        [Test]
        public void TestLoginWrongPsw()
        {
            var logged = mp.ValidateUser(specialUN, specialPSW + "1");
            Assert.IsFalse(logged);

        }

        [Test]
        public void TestLoginWrongUser()
        {
            var logged = mp.ValidateUser(specialUN + "111", specialPSW);
            Assert.IsFalse(logged);
        }

        [Test]
        public void TestLoginInPerformance()
        {
            DateTime now = DateTime.Now;
            for (int i = 0; i < 1000; i++)
            {
                var logged = mp.ValidateUser(specialUN + "ss", specialPSW);
                Assert.IsFalse(logged);
            }
            Console.WriteLine("{0} login attempts took {1}", 1000, (DateTime.Now - now));
        }

        [Test]
        public void TestMSSQLLoginInPerformance()
        {
            DateTime now = DateTime.Now;
            for (int i = 0; i < 1000; i++)
            {
                var logged = smp.ValidateUser(specialUN + "ss", specialPSW);
                Assert.IsFalse(logged);
            }
            Console.WriteLine("{0} login attempts took {1}", 1000, (DateTime.Now - now));
        }

        [Test]
        public void Test10LoginWrongPsw()
        {
            MembershipUser user = mp.GetUser(specialUN, false);
            Assert.IsNotNull(user);
            for(int i=0;i<mp.MaxInvalidPasswordAttempts + 5;i++)
            {
                var logged = mp.ValidateUser(specialUN, specialPSW + "1");
                Assert.IsFalse(logged);
                user = mp.GetUser(specialUN, false);
                Assert.IsNotNull(user);
                if (user.IsLockedOut)
                {
                    Assert.Pass("User has been locked after " + i + " unsuccessfull tries!");
                    return;
                }
            }
            Assert.Fail("User hasn't been locked after {0} failed tries", mp.MaxInvalidPasswordAttempts + 5);
        }

        [Test]
        public void ChangePswTest()
        {
            var user = mp.GetUser(specialUN, false);
            Assert.IsNotNull(user);
            user.ChangePassword(specialPSW, specialPSW + "lala");
            Assert.IsFalse(mp.ValidateUser(specialUN, specialPSW));
            Assert.IsTrue(mp.ValidateUser(specialUN, specialPSW + "lala"));
            user.ChangePassword(specialPSW + "lala", specialPSW);
            Assert.IsTrue(mp.ValidateUser(specialUN, specialPSW));
            Assert.IsFalse(mp.ValidateUser(specialUN, specialPSW + "lala"));
        }

        [Test]
        public void TestGetUsersByEmail()
        {
            int lala;
            var users = mp.FindUsersByEmail(specialUN, 0, 10, out lala);
            Assert.IsTrue(users.Count > 0);
            Assert.AreEqual(lala, users.Count);
        }

        [Test]
        public void UnlockUserTest()
        {
            var user = mp.GetUser(specialUN, false);
            Assert.IsNotNull(user);
            if (!user.IsLockedOut)
                Test10LoginWrongPsw();
            user = mp.GetUser(specialUN, false);
            Assert.IsNotNull(user);
            Assert.IsTrue(user.IsLockedOut);

            user.UnlockUser();
            user = mp.GetUser(specialUN, false);
            Assert.IsNotNull(user);
            Assert.IsFalse(user.IsLockedOut);
        }

        [Test]
        public void OnlineUsersTest()
        {
            var user = mp.GetUser(specialUN, true);
            Assert.IsNotNull(user);
            Assert.IsTrue(mp.GetNumberOfUsersOnline() > 0);
        }

        [Test]
        public void TestCreatingUsers2()
        {
            string user = Guid.NewGuid().ToString().Substring(0, 12);
            MembershipCreateStatus status;
            var muser = mp.CreateUser(user, "lalagugu1", user + "@" + domain, null, null, true, null, out status);
            Assert.IsNotNull(muser);
            Assert.AreEqual(muser.Email, user + "@" + domain);
            Assert.AreEqual(muser.UserName, user);

            var myuser = mp.GetUser(user, false) as MembershipUser;
            Assert.IsNotNull(myuser);
            //Assert.IsNotNull(mp.GetUser(myuser.id_user, false));

            Assert.IsTrue(mp.DeleteUser(user, true));
            Assert.IsNull(mp.GetUser(user, false));
        }

        [Test]
        public void ResetPasswordTest()
        {
            var user = mp.GetUser(specialUN, false);
            Assert.IsNotNull(user);
            if(user.IsLockedOut)
            {
                UnlockUserTest();
            }
            string newPsw = user.ResetPassword(null);
            Assert.IsTrue(mp.ValidateUser(specialUN, newPsw));
            Assert.IsFalse(mp.ValidateUser(specialUN, specialPSW));
            user.ChangePassword(newPsw, specialPSW);
            Assert.IsTrue(mp.ValidateUser(specialUN, specialPSW));
            Assert.IsFalse(mp.ValidateUser(specialUN, newPsw));
        }
    }
}
