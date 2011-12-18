using System;
using PureDev.Common;

namespace PureMembershipProviderManager
{
    public class Manager
    {
        private PureMembershipProvider _mp = new PureMembershipProvider();
        private PureRoleProvider _rp = new PureRoleProvider();

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
                        case "role":
                            ListRoles();
                            break;
                        case "user":
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
        }

        private void CreateUser(string name)
        {
            Console.WriteLine("Enter password for user {0}:", name);
            Console.Write("Password:");
            string password = Console.ReadLine().Trim();
            Console.WriteLine("Enter email for user {0}:", name);
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