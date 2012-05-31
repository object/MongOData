using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using NUnit.Framework;
using Simple.Data;
using Simple.Data.OData;

namespace Mongo.Context.Tests
{
    public abstract class BsonIdTests<T> : TestBase<T>
    {
        protected override void PopulateTestData()
        {
            TestData.PopulateWithBsonIdTypes();
        }

        [Test]
        public void Metadata()
        {
            var request = (HttpWebRequest)WebRequest.Create(service.ServiceUri + "/$metadata");
            var response = (HttpWebResponse)request.GetResponse();
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "The $metadata didn't return success.");
        }

        [Test]
        public void AllTypesWithoutExplicitIdVerifyResultCountAndId()
        {
            var result = ctx.TypeWithoutExplicitId.All().ToList();
            Assert.AreEqual(3, result.Count, "The service returned unexpected number of results.");
            Assert.IsNotNull(result[0].db_id);
        }

        [Test]
        public void AllTypesWithBsonIdVerifyResultCountAndId()
        {
            var result = ctx.TypeWithIntId.All().ToList();
            Assert.AreEqual(3, result.Count, "The service returned unexpected number of results.");
            Assert.IsNotNull(result[0].db_id);
        }

        [Test]
        public void AllTypesWithIntIdVerifyResultCountAndId()
        {
            var result = ctx.TypeWithIntId.All().ToList();
            Assert.AreEqual(3, result.Count, "The service returned unexpected number of results.");
            Assert.AreEqual("1", result[0].db_id);
        }

        [Test]
        public void AllTypesWithStringIdVerifyResultCountAndId()
        {
            var result = ctx.TypeWithStringId.All().ToList();
            Assert.AreEqual(3, result.Count, "The service returned unexpected number of results.");
            Assert.AreEqual("1", result[0].db_id);
        }

        [Test]
        public void AllTypesWithGuidIdVerifyResultCountAndId()
        {
            var result = ctx.TypeWithGuidId.All().ToList();
            Assert.AreEqual(3, result.Count, "The service returned unexpected number of results.");
            Assert.AreNotEqual(Guid.Empty.ToString(), result[0].db_id);
        }
    }

    [TestFixture]
    public class InMemoryServiceBsonIdTests : BsonIdTests<ProductInMemoryService>
    {
    }

    [TestFixture]
    public class QueryableServiceBsonIdTests : BsonIdTests<ProductQueryableService>
    {
    }
}
