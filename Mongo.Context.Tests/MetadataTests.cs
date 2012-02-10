using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Mongo.Context;

namespace Mongo.Context.Tests
{
    [TestFixture]
    public class MetadataTests
    {
        private string connectionString;

        [SetUp]
        public void SetUp()
        {
            this.connectionString = ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString;
        }

        [Test]
        public void should_create_key_value_context()
        {
            var context = MongoDataService<MongoKeyValueContext, MongoKeyValueMetadata>.CreateDataContext(this.connectionString);
            Assert.IsNotNull(context);
        }

        [Test]
        public void should_create_strong_type_context()
        {
            var context = MongoDataService<MongoStrongTypeContext, MongoStrongTypeMetadata>.CreateDataContext(this.connectionString);
            Assert.IsNotNull(context);
        }
    }
}
