using System;
using NUnit.Framework;

namespace Mongo.Context.Tests
{
    public abstract class BsonIdTests<T> : TestBase<T>
    {
        protected override void PopulateTestData()
        {
            TestData.PopulateWithBsonIdTypes();
        }

        [Test]
        public void ValidateMetadata()
        {
            base.RequestAndValidateMetadata();
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
            var result = ctx.TypeWithBsonId.All().ToList();
            Assert.AreEqual(3, result.Count, "The service returned unexpected number of results.");
            Assert.IsNotNull(result[0].db_id);
        }

        [Test]
        public void AllTypesWithIntIdVerifyResultCountAndId()
        {
            var result = ctx.TypeWithIntId.All().ToList();
            Assert.AreEqual(3, result.Count, "The service returned unexpected number of results.");
            Assert.AreEqual(1, result[0].db_id);
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
