using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PureDev.Common;

namespace PureMPTests
{
    [TestFixture]
    public class PureMySQLTests : MembershipProviderTests
    {
        private DateTime _startedDate;

        public PureMySQLTests()
        {
            ProviderName = "PureMembershipProvider";
        }

        [TestFixtureSetUp]
        public new void Init()
        {
            base.Init();
            _startedDate = DateTime.Now;
        }

        [TestFixtureTearDown]
        public new void TearDown()
        {
            Console.WriteLine("All tests for MySQL took {0}", DateTime.Now - _startedDate); ;
        }
    }

    [TestFixture]
    public class PureMSSQLTests : MembershipProviderTests
    {
        private DateTime _startedDate;

        public PureMSSQLTests()
        {
            ProviderName = "CustomSqlMembershipProvider";
        }

        [TestFixtureSetUp]
        public new void Init()
        {
            base.Init();
            _startedDate = DateTime.Now;
        }

        [TestFixtureTearDown]
        public new void TearDown()
        {
            Console.WriteLine("All tests for MySQL took {0}", DateTime.Now - _startedDate); ;
        }
    }
}
