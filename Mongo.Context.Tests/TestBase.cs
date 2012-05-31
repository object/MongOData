using System.Net;
using NUnit.Framework;
using Simple.Data;
using Simple.Data.OData;

namespace Mongo.Context.Tests
{
    public abstract class TestBase<T>
    {
        protected TestService service;
        protected dynamic ctx;

        protected abstract void PopulateTestData();

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            PopulateTestData();
            service = new TestService(typeof(T));
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            if (service != null)
            {
                service.Dispose();
                service = null;
            }

            TestData.Clean();
        }

        [SetUp]
        public virtual void SetUp()
        {
            ctx = Database.Opener.Open(service.ServiceUri);
        }
    }
}
