

using System;
using NUnit.Framework;

namespace Mongo.Context.Tests
{
    public abstract class UpdateTests<T> : TestBase<T>
    {
        protected override void PopulateTestData()
        {
            TestData.PopulateWithCategoriesAndProducts();
        }

        [Test]
        public void Insert()
        {
            ctx.Products.Insert(ID: 101, Name: "Test", Description: "Red apple", ReleaseDate: new DateTime(2009, 9, 13));

            var product = ctx.Products.Find(ctx.Products.ID == 101);
            Assert.IsNotNull(product, "No such product was found.");
            Assert.AreEqual("Test", product.Name, "The new product doesn't have correct name.");
        }

        [Test]
        public void UpdateSingleField()
        {
            ctx.Products.Insert(ID: 102, Name: "Test", Description: "Red apple", ReleaseDate: new DateTime(2009, 9, 13));
            var product = ctx.Products.Find(ctx.Products.ID == 102);

            ctx.Products.UpdateByID(ID: 102, ReleaseDate: product.ReleaseDate.AddDays(1));

            product = ctx.Products.Find(ctx.Products.ID == 102);
            Assert.IsNotNull(product, "No such product was found.");
            Assert.AreEqual(14, product.ReleaseDate.Day, "The update product doesn't have correct ReleaseDate.");
        }

        [Test]
        public void UpdateMultipleFields()
        {
            ctx.Products.Insert(ID: 103, Name: "Test", Description: "Red apple", ReleaseDate: new DateTime(2009, 9, 13));
            var product = ctx.Products.Find(ctx.Products.ID == 103);

            ctx.Products.UpdateByID(ID: 103, Description: "Green apple", ReleaseDate: product.ReleaseDate.AddDays(1));

            product = ctx.Products.Find(ctx.Products.ID == 103);
            Assert.IsNotNull(product, "No such product was found.");
            Assert.AreEqual("Green apple", product.Description, "The update product doesn't have correct Description.");
            Assert.AreEqual(14, product.ReleaseDate.Day, "The update product doesn't have correct ReleaseDate.");
        }

        [Test]
        public void Delete()
        {
            ctx.Products.Insert(ID: 104, Name: "Test", Description: "Red apple", ReleaseDate: new DateTime(2009, 9, 13));

            ctx.Products.Delete(ID: 104);

            // Ask for the entity we just deleted
            var product = ctx.Products.Find(ctx.Products.ID == 104);
            Assert.IsNull(product, "Product should be deleted.");
        }
    }

    [TestFixture]
    public class InMemoryServiceUpdateTests : UpdateTests<ProductInMemoryService>
    {
    }

    [TestFixture]
    public class QueryableServiceUpdateTests : UpdateTests<ProductQueryableService>
    {
    }
}
