using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using NUnit.Framework;
using Simple.Data;
using Simple.Data.OData;
using Simple.OData.Client;

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

        protected ISchema GetSchema()
        {
            var client = new ODataClient(service.ServiceUri.AbsoluteUri);
            return client.Schema;
        }

        protected void ValidateSchema(ISchema schema)
        {
            var edmTypeNames = typeof (EdmType).GetFields().Select(x => x.Name).ToList();
            foreach (var table in schema.Tables)
            {
                var key = table.PrimaryKey.AsEnumerable();
                foreach (var column in table.Columns)
                {
                    if (key.Contains(column.ActualName))
                        Assert.False(column.IsNullable, "Column {0} belongs to a primary key and should not be marked as nullable", column.ActualName);
                    else if (column.PropertyType.GetType().Name == "Edm.String")
                        Assert.True(column.IsNullable, "Column {0} is a string type and should be marked as nullable", column.ActualName);
                    else
                        Assert.False(column.IsNullable, "Column {0} of type {1} should not be marked as nullable", column.ActualName, column.PropertyType);
                }
            }
        }
    }
}
