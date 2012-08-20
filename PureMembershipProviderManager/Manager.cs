using System;
using System.Web.Security;
using PureDev.Common;

namespace PureMembershipProviderManager
{
    public class Manager
    {
        private PureMembershipProvider _mp;
        private RoleProvider _rp;

        protected const string MPProviderName = "PureMembershipProvider";
        protected const string RPName = "PureRoleProvider";

        public Manager()
        {
            _mp = (PureMembershipProvider)Membership.Providers[MPProviderName];
            //_rp = Roles.Providers[RPName];
        }

        public void ParseCommand(string command)
        {
            var commandParts = command.Split(" ".ToCharArray());
            if (commandParts.Length <= 1)
                return;

            var secondArg = commandParts[1].ToLower();
            if (secondArg != "user" && secondArg != "role" && secondArg != "users" && secondArg != "roles")
            {
                Console.WriteLine("Couldn't recognize second argument!");
                return;
            }

            switch (commandParts[0].ToLower())
            {
                case "create":
                    switch (commandParts[1].ToLower())
                    {
                        case "role":
                            CreateRole(commandParts[2]);
                            break;
                        case "user":
                            CreateUser(commandParts[2]);
                            break;
                    }
                    break;
                case "update":
                    switch (commandParts[1].ToLower())
                    {
                        case "role":
                            UpdateRole(commandParts[2]);
                            break;
                        case "user":
                            UpdateUser(commandParts[2]);
                            break;
                    }
                    break;
                case "list":
                    switch (commandParts[1].ToLower())
                    {
                        case "roles":
                            ListRoles();
                            break;
                        case "users":
                            ListUsers();
                            break;
                    }
                    break;
            }
        }

        private void ListRoles()
        {
        }

        private void ListUsers()
        {
            Console.WriteLine("List of all users");
            int userCount = 0;
            var userCollection = _mp.GetAllUsers(0, 100, out userCount);
            int i = 1;
            foreach (MembershipUser user in userCollection)
            {
                Console.WriteLine("{0,3} {1,10} {2,10}", i++, user.UserName, user.Email);
            }
        }

        private void CreateUser(string name)
        {
            Console.WriteLine("Creating user {0}:", name);
            Console.Write("Password:");
            string password = Console.ReadLine().Trim();
            Console.Write("E-mail:");
            string email = Console.ReadLine().Trim();

            _mp.CreateUser(name, email, password, true);
            Console.WriteLine("User has been successfully created!");
        }

        private void UpdateUser(string name)
        {
        }

        private void CreateRole(string name)
        {
        }

        private void UpdateRole(string name)
        {
        }
    }
}