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
        class Metadata
        {
            public string result_type { get; set; }
            public int recent_retweets { get; set; }
        }

        private class Result
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

        class Twitter
        {
            [BsonId]
            public ObjectId Id { get; set; }
            public Result results { get; set; }
        }

        protected override void PopulateTestData()
        {
            TestData.PopulateWithJsonSamples();
        }

        [SetUp]
        public override void SetUp()
        {
            TestService.Configuration = new MongoConfiguration { MetadataBuildStrategy = new MongoConfiguration.Metadata { PrefetchRows = -1, UpdateDynamically = false } };
            base.SetUp();
        }

        [Test]
        public void AllFromTwitter()
        {
            var database = TestData.OpenDatabase();
            var collection = database.GetCollection<Twitter>("Twitter");
            var results = collection.AsQueryable<Twitter>();
            Assert.AreEqual(1, results.Count());
            var result = results.First();
            Assert.AreEqual(1478555574, result.results.id);
        }
    }
}
