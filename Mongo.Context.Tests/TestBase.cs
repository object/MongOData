using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
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

        protected void RequestAndValidateMetadata()
        {
            var request = (HttpWebRequest)WebRequest.Create(service.ServiceUri + "$metadata");
            var response = (HttpWebResponse)request.GetResponse();
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "The $metadata didn't return success.");
        }
    }
}
