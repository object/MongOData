

using NUnit.Framework;

namespace Mongo.Context.Tests
{
    public abstract class ReferenceQueryTests<T> : TestBase<T>
    {
        protected override void PopulateTestData()
        {
            TestData.PopulateWithCategoriesAndProducts();
        }

        //[Test]
        //public void ResourceReferenceProperty()
        //{
        //    List<ClientCategory> categories = ctx.CreateQuery<ClientCategory>("Categories").ToList();
        //    var category = RunSingleResultQuery<ClientCategory>("/Products(0)/Category");
        //    Assert.AreEqual(categories[0], category, "The product ID 0 should be in the first category.");
        //    category = RunSingleResultQuery<ClientCategory>("/Products(1)/Category");
        //    Assert.AreEqual(categories[1], category, "The product ID 1 should be in the second category.");
        //    category = RunSingleResultQuery<ClientCategory>("/Products(2)/Category");
        //    Assert.AreEqual(categories[1], category, "The product ID 2 should be in the second category.");
        //}

        //[Test]
        //public void ResourceSetReferenceProperty()
        //{
        //    List<ClientProduct> products = ctx.CreateQuery<ClientProduct>("Products").ToList();
        //    var relatedProducts = RunQuery<ClientProduct>("/Categories(0)/Products").ToList();
        //    Assert.AreEqual(1, relatedProducts.Count, "Category 0 should have just one product.");
        //    Assert.IsTrue(relatedProducts.Contains(products[0]), "The category 0 should have product 0 in it.");
        //    relatedProducts = RunQuery<ClientProduct>("/Categories(1)/Products").ToList();
        //    Assert.AreEqual(2, relatedProducts.Count, "Category 1 should have two products.");
        //    Assert.IsTrue(relatedProducts.Contains(products[1]), "The category 1 should have product 1 in it.");
        //    Assert.IsTrue(relatedProducts.Contains(products[2]), "The category 1 should have product 2 in it.");
        //    relatedProducts = RunQuery<ClientProduct>("/Categories(2)/Products").ToList();
        //    Assert.AreEqual(0, relatedProducts.Count, "Category 2 should have no products.");
        //}
    }

    [TestFixture]
    public class InMemoryServiceReferenceQueryTests : ReferenceQueryTests<ProductInMemoryService>
    {
    }

    [TestFixture]
    public class QueryableServiceReferenceQueryTests : ReferenceQueryTests<ProductQueryableService>
    {
    }
}
