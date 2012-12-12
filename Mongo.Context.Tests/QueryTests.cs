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
    public abstract class QueryTests<T> : TestBase<T>
    {
        protected override void PopulateTestData()
        {
            TestData.PopulateWithCategoriesAndProducts();
        }

        [SetUp]
        public override void SetUp()
        {
            TestService.Configuration = new MongoConfiguration { MetadataBuildStrategy = new MongoConfiguration.Metadata { PrefetchRows = -1, UpdateDynamically = false } };
            base.SetUp();
        }

        [Test]
        public void ValidateMetadata()
        {
            base.RequestAndValidateMetadata();
        }

        [Test]
        public void SchemaTables()
        {
            var schema = base.GetSchema();
            var tableNames = schema.Tables.Select(x => x.ActualName).ToList();
            Assert.Contains("Products", tableNames);
            Assert.Contains("Categories", tableNames);
            Assert.Contains("ClrTypes", tableNames);
        }

        [Test]
        public void SchemaColumnNullability()
        {
            var schema = base.GetSchema();
            base.ValidateSchema(schema);
        }

        [Test]
        public void AllEntitiesVerifyResultCount()
        {
            var result = ctx.Products.All().ToList();
            Assert.AreEqual(3, result.Count, "The service returned unexpected number of results.");
        }

        [Test]
        public void AllEntitiesTakeOneVerifyResultCount()
        {
            var result = ctx.Products.All().Take(1).ToList();
            Assert.AreEqual(1, result.Count, "The service returned unexpected number of results.");
        }

        [Test]
        public void AllEntitiesSkipOneVerifyResultCount()
        {
            var result = ctx.Products.All().Skip(1).ToList();
            Assert.AreEqual(2, result.Count, "The service returned unexpected number of results.");
        }

        [Test]
        public void AllEntitiesCountVerifyResult()
        {
            var result = ctx.Products.All().Count();
            Assert.AreEqual(3, result, "The count is not correctly computed.");
        }

        [Test]
        public void AllEntitiesVerifyID()
        {
            var result = ctx.Products.All().ToList();
            Assert.AreEqual(3, result[2].ID, "The ID is not correctly filled.");
        }

        [Test]
        public void AllEntitiesVerifyProductName()
        {
            var result = ctx.Products.All().ToList();
            Assert.AreEqual("Milk", result[1].Name, "The Product Name is not correctly filled.");
        }

        [Test]
        public void AllEntitiesVerifySupplierName()
        {
            var result = ctx.Products.All().ToList();
            Assert.AreEqual("Green Farm", result[1].Supplier.Name, "The Supplier Name is not correctly filled.");
        }

        [Test]
        public void AllEntitiesVerifyReleaseDate()
        {
            var result = ctx.Products.All().ToList();
            Assert.AreEqual(new DateTime(1992, 1, 1), result[0].ReleaseDate, "The ReleaseDate is not correctly filled.");
        }

        [Test]
        public void AllEntitiesVerifyNullDiscontinueDate()
        {
            var result = ctx.Products.All().ToList();
            Assert.Null(result[0].DiscontinueDate, "The DiscontinueDate must be null.");
            Assert.Null(result[1].DiscontinueDate, "The DiscontinueDate must be null.");
        }

        [Test]
        public void AllEntitiesVerifyNonNullDiscontinueDate()
        {
            var result = ctx.Products.All().ToList();
            Assert.NotNull(result[2].DiscontinueDate, "The DiscontinueDate must not be null.");
        }

        [Test]
        public void AllEntitiesOrderby()
        {
            var result = ctx.Products.All().OrderBy(ctx.Products.Name).ToList();
            for (int i = 0; i < 2; i++)
            {
                Assert.Greater(result[i + 1].Name, result[i].Name, "Names are not in correct order.");
            }
        }

        [Test]
        public void AllEntitiesOrderbyDescending()
        {
            var result = ctx.Products.All().OrderByDescending(ctx.Products.Name).ToList();
            for (int i = 0; i < 2; i++)
            {
                Assert.Less(result[i + 1].Name, result[i].Name, "Names are not in correct order.");
            }
        }

        [Test]
        public void AllEntitiesOrderbyTakeOneVerifyResultCount()
        {
            var result = ctx.Products.All().OrderBy(ctx.Products.Name).Take(1).ToList();
            Assert.AreEqual(1, result.Count, "The service returned unexpected number of results.");
        }

        [Test]
        public void AllEntitiesVerifyQuantityValue()
        {
            var result = ctx.Products.All().ToList();
            Assert.AreEqual(12, result[0].Quantity.Value, "Unexpected quantity value.");
        }

        [Test]
        public void FilterEqualID()
        {
            var product = ctx.Products.Find(ctx.Products.ID == 1);
            Assert.AreEqual(1, product.ID);
        }

        [Test]
        public void FilterEqualName()
        {
            var result = ctx.Products.FindAll(ctx.Products.Name == "Bread").ToList();
            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void FilterEqualNameCountVerifyResult()
        {
            var result = ctx.Products.FindAll(ctx.Products.Name == "Bread").Count();
            Assert.AreEqual(1, result, "The count is not correctly computed.");
        }

        [Test]
        public void FilterEqualIDAndEqualName()
        {
            var result = ctx.Products.FindAll(ctx.Products.ID == 1 && ctx.Products.Name == "Bread").ToList();
            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void FilterGreaterID()
        {
            var result = ctx.Products.FindAll(ctx.Products.ID > 0).ToList();
            Assert.AreEqual(3, result.Count);
        }

        [Test]
        public void FilterNameContainsEqualsTrue()
        {
            var result = ctx.Products.FindAll(ctx.Products.Name.Contains("i") == true).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void FilterNameContainsEqualsFalse()
        {
            var result = ctx.Products.FindAll(ctx.Products.Name.Contains("i") == false).ToList();
            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void FilterGreaterRating()
        {
            var result = ctx.Products.FindAll(ctx.Products.Rating > 3).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void FilterNameLength()
        {
            var result = ctx.Products.FindAll(ctx.Products.Name.Length() == 4).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void FilterGreaterIDAndNameLength()
        {
            var result = ctx.Products.FindAll(ctx.Products.ID > 0 && ctx.Products.Name.Length() == 4).ToList();
            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void FilterNameLengthOrderByCountVerifyResult()
        {
            var result = ctx.Products.FindAll(ctx.Products.Name.Length() == 4).OrderBy(ctx.Products.Rating).Count();
            Assert.AreEqual(2, result, "The count is not correctly computed.");
        }

        [Test]
        public void FilterEqualQuantityValue()
        {
            var result = ctx.Products.Find(ctx.Products.Quantity.Value == 7);
            Assert.AreEqual("Wine", result.Name);
        }

        [Test]
        public void FilterEqualQuantityUnits()
        {
            var result = ctx.Products.Find(ctx.Products.Quantity.Units == "liters");
            Assert.AreEqual("Milk", result.Name);
        }

        [Test]
        public void FilterEqualObjectID()
        {
            var product = ctx.Products.Find(ctx.Products.ID == 1);
            product = ctx.Products.Find(ctx.Products.db_id == product.db_id);
            Assert.AreEqual(1, product.ID);
        }

        [Test]
        public void ProjectionVerifyExcluded()
        {
            var product = ctx.Products.All().Select(ctx.Products.ID).First();
            var id = product.ID;
            try
            {
                var name = product.Name;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
            }
        }

        [Test]
        public void ProjectionVerifyID()
        {
            var products = ctx.Products.FindAll(ctx.Products.ID > 0).Select(ctx.Products.ID).ToList();
            foreach (var product in products)
            {
                Assert.Greater(product.ID, 0, "The ID is not correctly filled.");
            }
        }

        [Test]
        public void ProjectionVerifyName()
        {
            var products = ctx.Products.All().Select(ctx.Products.Name).ToList();
            foreach (var product in products)
            {
                Assert.IsNotNull(product.Name, "The Name is not correctly filled.");
            }
        }

        [Test]
        public void ProjectionVerifyQuantity()
        {
            var products = ctx.Products.All().Select(ctx.Products.Quantity).ToList();
            foreach (var product in products)
            {
                Assert.Greater(product.Quantity.Value, 0, "The Quantity is not correctly filled.");
            }
        }

        [Test]
        public void ProjectionVerifyNameDescriptionRating()
        {
            var products = ctx.Products.All().Select(ctx.Products.Name, ctx.Products.Description, ctx.Products.Rating).ToList();
            foreach (var product in products)
            {
                Assert.IsNotNull(product.Name, "The Name is not correctly filled.");
                Assert.IsNotNull(product.Description, "The Description is not correctly filled.");
                Assert.Greater(product.Rating, 0, "The Rating is not correctly filled.");
            }
        }

        [Test]
        public void VerifyClrTypes()
        {
            var clr = ctx.ClrTypes.All().First();
            Assert.AreEqual(new[] { (byte)1 }, clr.BinaryValue, "The BinaryValue is not correctly filled.");
            Assert.AreEqual(true, clr.BoolValue, "The BoolValue is not correctly filled.");
            Assert.AreEqual(new DateTime(2012, 1, 1), clr.DateTimeValue, "The DateTimeValue is not correctly filled.");
            Assert.AreEqual("01:02:03", clr.TimeSpanValue, "The TimeSpan is not correctly filled.");
            Assert.AreEqual(Guid.Empty, clr.GuidValue, "The GuidValue is not correctly filled.");
            Assert.AreEqual(1, clr.ByteValue, "The ByteValue is not correctly filled.");
            Assert.AreEqual(2, clr.SByteValue, "The SByteValue is not correctly filled.");
            Assert.AreEqual(3, clr.Int16Value, "The Int16Value is not correctly filled.");
            Assert.AreEqual(4, clr.UInt16Value, "The UInt16Value is not correctly filled.");
            Assert.AreEqual(5, clr.Int32Value, "The Int32Value is not correctly filled.");
            Assert.AreEqual(6, clr.UInt32Value, "The UInt32Value is not correctly filled.");
            Assert.AreEqual(7, clr.Int64Value, "The Int64Value is not correctly filled.");
            Assert.AreEqual(8, clr.UInt64Value, "The UInt64Value is not correctly filled.");
            Assert.AreEqual(9, clr.SingleValue, "The SingleValue is not correctly filled.");
            Assert.AreEqual(10, clr.DoubleValue, "The DoubleValue is not correctly filled.");
            Assert.AreEqual("11", clr.DecimalValue, "The DecimalValue is not correctly filled.");
            Assert.AreEqual("abc", clr.StringValue, "The StringValue is not correctly filled.");
        }
    }

    [TestFixture]
    public class InMemoryServiceQueryTests : QueryTests<ProductInMemoryService>
    {
        [Test]
        public void SchemaColumnNullability1()
        {
            var schema = base.GetSchema();
            base.ValidateSchema(schema);
        }
    }

    [TestFixture]
    public class QueryableServiceQueryTests : QueryTests<ProductQueryableService>
    {
        [Test]
        public void FilterEqualObjectID1()
        {
            var product = ctx.Products.Find(ctx.Products.ID == 1);
            product = ctx.Products.Find(ctx.Products.db_id == product.db_id);
            Assert.AreEqual(1, product.ID);
        }

        [Test]
        public void Test()
        {
            var serviceUri = "http://localhost:5555/OdaWeb";
            var db = Database.Opener.Open(serviceUri);
            var result = db.EventStore.All().Take(10).ToList();
            Assert.AreEqual(10, result.Count, "The service returned unexpected number of results.");
        }
    }
}
