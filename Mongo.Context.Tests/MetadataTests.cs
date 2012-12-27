using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CSharp.RuntimeBinder;
using NUnit.Framework;
using Simple.Data;
using Simple.Data.OData;

namespace Mongo.Context.Tests
{
    public abstract class MetadataTests<T> : TestBase<T>
    {
        protected override void PopulateTestData()
        {
            TestData.PopulateWithVariableTypes();
        }

        [SetUp]
        public override void SetUp()
        {
            ProductInMemoryService.ResetDSPMetadata();
            ProductQueryableService.ResetDSPMetadata();
            base.SetUp();
        }

        protected void ResetService()
        {
            service.Dispose();
            service = new TestService(typeof(T));
            ctx = Database.Opener.Open(service.ServiceUri);
        }

        [Test]
        public void ValidateMetadata()
        {
            base.RequestAndValidateMetadata();
        }

        [Test]
        public void VariableTypesPrefetchOneNoUpdate()
        {
            TestService.Configuration = new MongoConfiguration { MetadataBuildStrategy = new MongoConfiguration.Metadata { PrefetchRows = 1, UpdateDynamically = false } };
            ResetService();

            var result = ctx.VariableTypes.All().ToList();
            AssertResultHasOneColumn(result);
        }

        [Test]
        public void VariableTypesPrefetchTwoNoUpdate()
        {
            TestService.Configuration = new MongoConfiguration { MetadataBuildStrategy = new MongoConfiguration.Metadata { PrefetchRows = 2, UpdateDynamically = false } };
            ResetService();

            var result = ctx.VariableTypes.All().ToList();
            AssertResultHasTwoColumns(result);
        }

        [Test]
        public void VariableTypesPrefetchAll()
        {
            TestService.Configuration = new MongoConfiguration { MetadataBuildStrategy = new MongoConfiguration.Metadata { PrefetchRows = -1, UpdateDynamically = false } };
            ResetService();

            var result = ctx.VariableTypes.All().ToList();
            AssertResultHasThreeColumns(result);
        }

        protected void AssertResultHasNoColumns(IList<dynamic> result)
        {
            Assert.Throws<RuntimeBinderException>(() => { var x = result[0].StringValue; });
        }

        protected void AssertResultHasOneColumn(IList<dynamic> result)
        {
            Assert.AreEqual("1", result[0].StringValue);
            Assert.Throws<RuntimeBinderException>(() => { var x = result[1].IntValue; });
        }

        protected void AssertResultHasTwoColumns(IList<dynamic> result)
        {
            Assert.AreEqual("1", result[0].StringValue);
            Assert.AreEqual(2, result[1].IntValue);
            Assert.Throws<RuntimeBinderException>(() => { var x = result[2].DecimalValue; });
        }

        protected void AssertResultHasThreeColumns(IList<dynamic> result)
        {
            Assert.AreEqual("1", result[0].StringValue);
            Assert.AreEqual(2, result[1].IntValue);
            Assert.AreEqual("3", result[2].DecimalValue);
        }
    }

    [TestFixture]
    public class InMemoryServiceMetadataTests : MetadataTests<ProductInMemoryService>
    {
    }

    [TestFixture]
    public class QueryableServiceMetadataTests : MetadataTests<ProductQueryableService>
    {
        [Test]
        public void VariableTypesPrefetchNoneUpdate()
        {
            TestService.Configuration = new MongoConfiguration { MetadataBuildStrategy = new MongoConfiguration.Metadata { PrefetchRows = 0, UpdateDynamically = true } };
            ResetService();

            var result = ctx.VariableTypes.All().ToList();
            AssertResultHasNoColumns(result);
            ResetService();

            result = ctx.VariableTypes.All().ToList();
            AssertResultHasThreeColumns(result);
        }

        [Test]
        public void VariableTypesPrefetchOneUpdate()
        {
            TestService.Configuration = new MongoConfiguration { MetadataBuildStrategy = new MongoConfiguration.Metadata { PrefetchRows = 1, UpdateDynamically = true } };
            ResetService();

            var result = ctx.VariableTypes.All().ToList();
            AssertResultHasOneColumn(result);
            ResetService();

            result = ctx.VariableTypes.All().ToList();
            AssertResultHasThreeColumns(result);
        }

        [Test]
        public void VariableTypesPrefetchTwoUpdate()
        {
            TestService.Configuration = new MongoConfiguration { MetadataBuildStrategy = new MongoConfiguration.Metadata { PrefetchRows = 2, UpdateDynamically = true } };
            ResetService();

            var result = ctx.VariableTypes.All().ToList();
            AssertResultHasTwoColumns(result);
            ResetService();

            result = ctx.VariableTypes.All().ToList();
            AssertResultHasThreeColumns(result);
        }
    }
}
