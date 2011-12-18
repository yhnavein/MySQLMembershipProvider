using System;

namespace PureMembershipProviderManager
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Possible actions:");
            Console.WriteLine("[create|update] [user|role] {{name}}");
            Console.WriteLine("list [users|roles]");
            Console.WriteLine("[quit|exit]");
            string rl;
            var m = new Manager();

            while (true)
            {
                rl = Console.ReadLine();
                if (string.IsNullOrEmpty(rl))
                    continue;
                rl = rl.Trim();

                if (rl.ToLower() == "quit" || rl.ToLower() == "exit")
                    break;

                m.ParseCommand(rl);
            }
            Console.WriteLine("Closing application");
        }
    }
}