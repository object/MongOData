using System.Configuration;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NUnit.Framework;

namespace Mongo.Context.Tests
{
    [TestFixture]
    public class NativeTests : TestBase<ProductInMemoryService>
    {
        class Product : ClientProduct
        {
            [BsonId]
            public ObjectId Id { get; set; }
        }

        class TwitterCollection
        {
            public class Metadata
            {
                public string result_type { get; set; }
                public int recent_retweets { get; set; }
            }

            public class Result
            {
                // TODO: remove when MongoDB LINQ provider is fixed
                [BsonId]
                public ObjectId Id { get; set; }

                public string text { get; set; }
                public int to_user_id { get; set; }
                public string to_user { get; set; }
                public string from_user { get; set; }
                public Metadata metadata { get; set; }
                public int id { get; set; }
                public int from_user_id { get; set; }
                public string iso_language_code { get; set; }
                public string source { get; set; }
                public string profile_image_url { get; set; }
                public string created_at { get; set; }
                public int since_id { get; set; }
                public int max_id { get; set; }
                public string refresh_url { get; set; }
                public int results_per_page { get; set; }
                public string next_page { get; set; }
                public double completed_in { get; set; }
                public int page { get; set; }
                public string query { get; set; }
            }

            [BsonId]
            public ObjectId Id { get; set; }
            public Result results { get; set; }
        }

        private MongoDatabase _database;

        protected override void PopulateTestData()
        {
            TestData.CreateDatabase();
            TestData.PopulateWithJsonSamples(false);
            TestData.PopulateWithCategoriesAndProducts(false);
        }

        [SetUp]
        public override void SetUp()
        {
            TestService.Configuration = new MongoConfiguration { MetadataBuildStrategy = new MongoConfiguration.Metadata { PrefetchRows = -1, UpdateDynamically = false } };
            base.SetUp();
            _database = TestData.OpenDatabase();
        }

        [Test]
        public void FilterEqualQuantityValue()
        {
            var collection = _database.GetCollection<Product>("Products");
            var result = collection.AsQueryable().Where(x => x.Quantity.Value == 7).Single();
            Assert.AreEqual("Wine", result.Name);
        }

        [Test]
        public void FilterEqualQuantityUnits()
        {
            var collection = _database.GetCollection<Product>("Products");
            var result = collection.AsQueryable().Where(x => x.Quantity.Units == "liters").Single();
            Assert.AreEqual("Milk", result.Name);
        }

        [Test]
        public void Twitter()
        {
            var collection = _database.GetCollection<TwitterCollection>("Twitter");
            var result = collection.AsQueryable();
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(1478555574, result.First().results.id);
        }
    }
}
