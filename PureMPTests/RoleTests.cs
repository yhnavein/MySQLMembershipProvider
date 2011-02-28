using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Security;
using NUnit.Framework;
using System.Configuration.Provider;

namespace PureDev.Common
{
    [TestFixture]
    public class RoleTests
    {
        PureMembershipProvider mp;
        SqlMembershipProvider smp;
        SqlRoleProvider srp;
        RoleProvider rp;
        const string specialUN = "malinek";
        const string specialRN = "malinkowaRola";
        const string specialRN1 = "malinkowaRola1";

        [TestFixtureSetUp]
        public void Init()
        {
            mp = (PureMembershipProvider) Membership.Providers["PureMembershipProvider"];
            smp = (SqlMembershipProvider)Membership.Providers["CustomSqlMembershipProvider"];

            rp = Roles.Providers["PureRoleProvider"];
            srp = (SqlRoleProvider)Roles.Providers["CustomSqlRoleProvider"];
        }

        private static void CreateSpecialRole(RoleProvider roleProvider)
        {
            if (!roleProvider.RoleExists(specialRN))
            {
                roleProvider.CreateRole(specialRN);
                Assert.IsTrue(roleProvider.RoleExists(specialRN));
            }
            else
                Assert.Pass("Special role is already created!");
        }

        [Test]
        public void CreateSpecialRoleTest()
        {
            CreateSpecialRole(rp);
        }

        [Test]
        public void CreateSpecialMSSQLRoleTest()
        {
            CreateSpecialRole(srp);
        }

        [Test]
        public void CreateRoleTest()
        {
            string roleName = Guid.NewGuid().ToString().Substring(0, 18).Replace("-", "");
            rp.CreateRole(roleName);
            Assert.IsTrue(rp.RoleExists(roleName));
        }

        [Test]
        [ExpectedException(typeof(ProviderException))]
        public void CreateDuplRoleTest()
        {
            if (!rp.RoleExists(specialRN))
                CreateSpecialRoleTest();

            try
            {
                rp.CreateRole(specialRN);
                Assert.Fail("Role provider let creating duplicate role!");
            }
            catch (ProviderException ex)
            {
                Assert.Pass("Role provider throws exception on duplicated role");
            }

        }

        [Test]
        public void AssignSpecialRoleToSpecialUserTest()
        {
            if (!rp.RoleExists(specialRN))
                CreateSpecialRoleTest();
            rp.AddUsersToRoles(new[] {specialUN}, new[] {specialRN});
            Assert.IsTrue(rp.IsUserInRole(specialUN, specialRN));
            TakeAwayFromSpecialUserTest();
        }

        [Test]
        public void RemoveRoleTest()
        {
            if(!rp.RoleExists(specialRN1))
                rp.CreateRole(specialRN1);
            Assert.IsTrue(rp.RoleExists(specialRN1));
            if (rp.RoleExists(specialRN1))
            {
                rp.DeleteRole(specialRN1, false);
            }
            Assert.IsFalse(rp.RoleExists(specialRN1));
        }

        [Test]
        public void TakeAwayFromSpecialUserTest()
        {
            if (rp.IsUserInRole(specialUN, specialRN))
            {
                rp.RemoveUsersFromRoles(new[] { specialUN }, new[] { specialRN });
            }
            Assert.IsFalse(rp.IsUserInRole(specialUN, specialRN));
        }

        [Test]
        public void PerformanceRoleTest()
        {
            int all;
            const int PERF_COUNT = 20;
            var users = mp.GetAllUsers(0, 10, out all);
            var usrStr = users.Cast<MembershipUser>().Select(p => p.UserName);
            var roles = new List<string>(PERF_COUNT);
            var now = DateTime.Now;
            for (int i = 0; i < PERF_COUNT; i++)
            {
                string roleName = Guid.NewGuid().ToString().Replace("-", "");
                rp.CreateRole(roleName);
                roles.Add(roleName);
            }
            Console.WriteLine("Creating {0} roles took {1}", PERF_COUNT, (DateTime.Now - now));
            now = DateTime.Now;
            rp.AddUsersToRoles(usrStr.ToArray(), roles.ToArray());
            Assert.AreEqual(rp.GetUsersInRole(roles.Last()).Length, users.Count);
            Console.WriteLine("Assigning {0} roles to {1} users operations took {2}", PERF_COUNT, users.Count, (DateTime.Now - now));
            now = DateTime.Now;
            rp.RemoveUsersFromRoles(usrStr.ToArray(), roles.ToArray());
            Assert.AreEqual(rp.GetUsersInRole(roles.Last()).Length, 0);
            for (int i = 0; i < PERF_COUNT; i++)
                rp.DeleteRole(roles[i], false);
            Console.WriteLine("Removing created roles and assignes took {0}", (DateTime.Now - now));
        }

        [Test]
        public void PerformanceMSSQLRoleTest()
        {
            int all;
            const int PERF_COUNT = 20;
            var users = smp.GetAllUsers(0, 10, out all);
            var usrStr = users.Cast<MembershipUser>().Select(p => p.UserName);
            var roles = new List<string>(PERF_COUNT);
            var now = DateTime.Now;
            for (int i = 0; i < PERF_COUNT; i++)
            {
                string roleName = Guid.NewGuid().ToString().Replace("-", "");
                srp.CreateRole(roleName);
                roles.Add(roleName);
            }
            Console.WriteLine("Creating {0} roles took {1}", PERF_COUNT, (DateTime.Now - now));
            now = DateTime.Now;
            srp.AddUsersToRoles(usrStr.ToArray(), roles.ToArray());
            Assert.AreEqual(srp.GetUsersInRole(roles.Last()).Length, users.Count);
            Console.WriteLine("Assigning {0} roles to {1} users operations took {2}", PERF_COUNT, users.Count, (DateTime.Now - now));
            now = DateTime.Now;
            srp.RemoveUsersFromRoles(usrStr.ToArray(), roles.ToArray());
            Assert.AreEqual(srp.GetUsersInRole(roles.Last()).Length, 0);
            for (int i = 0; i < PERF_COUNT; i++)
                srp.DeleteRole(roles[i], false);
            Console.WriteLine("Removing created roles and assignes took {0}", (DateTime.Now - now));
        }

        [Test]
        public void PerformanceRoleTest2()
        {
            int all;
            const int PERF_COUNT = 10;
            var users = mp.GetAllUsers(0, 10, out all);
            var usrStr = users.Cast<MembershipUser>().Select(p => p.UserName);
            var roles = new List<string>(PERF_COUNT);
            var now = DateTime.Now;
            for (int i = 0; i < PERF_COUNT; i++)
            {
                string roleName = Guid.NewGuid().ToString().Replace("-", "");
                rp.CreateRole(roleName);
                roles.Add(roleName);
            }
            Console.WriteLine("Creating {0} roles took {1}", PERF_COUNT, (DateTime.Now - now));
            now = DateTime.Now;
            rp.AddUsersToRoles(usrStr.ToArray(), roles.ToArray());
            Assert.AreEqual(rp.GetUsersInRole(roles.Last()).Length, users.Count);
            Console.WriteLine("Assigning {0} roles to {1} users operations took {2}", PERF_COUNT, users.Count, (DateTime.Now - now));
            now = DateTime.Now;
            for (int i = 0; i < PERF_COUNT; i++)
                rp.DeleteRole(roles[i], false);
            Assert.AreEqual(rp.GetUsersInRole(roles.Last()).Length, 0);
            Console.WriteLine("Removing created roles and assignes took {0}", (DateTime.Now - now));
        }

        [Test]
        public void RoleRemovingWithAssignesTest()
        {
            int all;
            var users = mp.GetAllUsers(0, 10, out all);
            var usrStr = users.Cast<MembershipUser>().Select(p => p.UserName);

            string roleName = Guid.NewGuid().ToString().Replace("-", "");
            rp.CreateRole(roleName);

            rp.AddUsersToRoles(usrStr.ToArray(), new[] { roleName });
            Assert.AreEqual(rp.GetUsersInRole(roleName).Length, users.Count);

            rp.DeleteRole(roleName, false);
            Assert.AreEqual(rp.GetUsersInRole(roleName).Length, 0);
        }

        [Test]
        public void RoleAttachTest()
        {
            string roleName = Guid.NewGuid().ToString().Replace("-", "");
            rp.CreateRole(roleName);
            Assert.IsTrue(rp.RoleExists(roleName));
            rp.AddUsersToRoles(new[] { specialUN }, new[] { roleName });
            Assert.IsTrue(rp.IsUserInRole(specialUN, roleName));
            rp.RemoveUsersFromRoles(new[] { specialUN }, new[] { roleName });
            Assert.IsFalse(rp.IsUserInRole(specialUN, roleName));

            rp.DeleteRole(roleName, false);
        }

        [Test]
        public void RoleAttachTest1()
        {
            string roleName = Guid.NewGuid().ToString().Replace("-", "");
            rp.CreateRole(roleName);
            Assert.IsTrue(rp.RoleExists(roleName));
            rp.AddUsersToRoles(new[] { specialUN }, new[] { roleName });
            Assert.Contains(roleName, rp.GetRolesForUser(specialUN));
            rp.RemoveUsersFromRoles(new[] { specialUN }, new[] { roleName });
            Assert.IsFalse(rp.IsUserInRole(specialUN, roleName));

            rp.DeleteRole(roleName, false);
        }

        [Test]
        public void RoleAttachTest2()
        {
            string roleName = Guid.NewGuid().ToString().Replace("-", "");
            rp.CreateRole(roleName);
            Assert.IsTrue(rp.RoleExists(roleName));
            rp.AddUsersToRoles(new[] { specialUN }, new[] { roleName });
            Assert.Contains(specialUN, rp.GetUsersInRole(roleName));
            rp.RemoveUsersFromRoles(new[] { specialUN }, new[] { roleName });
            Assert.IsFalse(rp.IsUserInRole(specialUN, roleName));

            rp.DeleteRole(roleName, false);
        }

        [Test]
        public void FindUsersInRoleTest()
        {
            if(!rp.RoleExists(specialRN1))
                rp.CreateRole(specialRN1);
            var userNames = new[] { "zaza123", "zaza321", "zqqa123" };
            foreach (var userName in userNames)
                mp.CreateUser(userName, "qwerty123", userName + "@gmail.com", true);

            rp.AddUsersToRoles(userNames, new[] { specialRN1 });
            var found = rp.FindUsersInRole(specialRN1, "zaza");
            Assert.AreEqual(found.Length, 2);
            Assert.IsFalse(found.Contains("zqqa123"));

            foreach (var userName in userNames)
                mp.DeleteUser(userName, false);
        }

    }
}
