using NUnit.Framework;

namespace Mongo.Context.Tests
{
    public abstract class ReferenceUpdateTests<T> : TestBase<T>
    {
        protected override void PopulateTestData()
        {
            TestData.PopulateWithCategoriesAndProducts();
        }

        //[Test]
        //public void ResourceReferenceProperty_UpdateLink()
        //{
        //    ClientProduct bread = this.ctx.CreateQuery<ClientProduct>("Products").Where(p => p.ID == 0).First();
        //    ClientCategory beverages = this.ctx.CreateQuery<ClientCategory>("Categories").Where(c => c.ID == 1).First();
        //    ctx.SetLink(bread, "Category", beverages);

        //    ctx.SaveChanges();

        //    // Ask for the bread and its category again
        //    bread =
        //        this.ctx.CreateQuery<ClientProduct>("Products").AddQueryOption("$expand", "Category").Where(
        //            p => p.ID == 0).First();
        //    Assert.IsNotNull(bread.Category, "The category of the bread is null.");
        //    Assert.AreEqual(1, bread.Category.ID, "The category of the bread is wrong.");
        //}

        //[Test]
        //public void ResourceReferenceProperty_AddLink()
        //{
        //    ClientProduct apple = new ClientProduct()
        //                              {
        //                                  ID = 101,
        //                                  Name = "Apple",
        //                                  Description = "Red apple",
        //                                  ReleaseDate = new DateTime(2009, 9, 13)
        //                              };
        //    ctx.AddObject("Products", apple);

        //    ClientCategory food = this.ctx.CreateQuery<ClientCategory>("Categories").Where(c => c.ID == 0).First();
        //    ctx.SetLink(apple, "Category", food);

        //    ctx.SaveChanges();

        //    // Ask for the apple and its category again
        //    apple =
        //        this.ctx.CreateQuery<ClientProduct>("Products").AddQueryOption("$expand", "Category").Where(
        //            p => p.ID == 101).First();
        //    Assert.IsNotNull(apple.Category, "The category of the apple is null.");
        //    Assert.AreEqual(0, apple.Category.ID, "The category of the apple is wrong.");
        //}

        //[Test]
        //public void ResourceReferenceProperty_DeleteLink()
        //{
        //    ClientProduct bread = this.ctx.CreateQuery<ClientProduct>("Products").Where(p => p.ID == 0).First();
        //    ctx.SetLink(bread, "Category", null);

        //    ctx.SaveChanges();

        //    // Ask for the bread and its category again
        //    bread =
        //        this.ctx.CreateQuery<ClientProduct>("Products").AddQueryOption("$expand", "Category").Where(
        //            p => p.ID == 0).First();
        //    Assert.IsNull(bread.Category, "The category of the bread is not null.");
        //}

        //[Test]
        //public void ResourceSetReferenceProperty_AddLink()
        //{
        //    ClientProduct apple = new ClientProduct()
        //                              {
        //                                  ID = 101,
        //                                  Name = "Apple",
        //                                  Description = "Red apple",
        //                                  ReleaseDate = new DateTime(2009, 9, 13)
        //                              };
        //    ctx.AddObject("Products", apple);

        //    ClientCategory food = this.ctx.CreateQuery<ClientCategory>("Categories").Where(c => c.ID == 0).First();
        //    ctx.AddLink(food, "Products", apple);

        //    ctx.SaveChanges();

        //    // Ask for the food and its products again
        //    food =
        //        this.ctx.CreateQuery<ClientCategory>("Categories").AddQueryOption("$expand", "Products").Where(
        //            p => p.ID == 0).First();
        //    Assert.IsTrue(food.Products.Any(p => p.ID == 101), "The food category doesn't have the new apple product.");
        //}

        //[Test]
        //public void ResourceSetReferenceProperty_DeleteLink()
        //{
        //    ClientProduct bread = this.ctx.CreateQuery<ClientProduct>("Products").Where(p => p.ID == 0).First();
        //    ClientCategory food = this.ctx.CreateQuery<ClientCategory>("Categories").Where(c => c.ID == 0).First();
        //    ctx.DeleteLink(food, "Products", bread);

        //    ctx.SaveChanges();

        //    // Ask for the food and its products again
        //    food =
        //        this.ctx.CreateQuery<ClientCategory>("Categories").AddQueryOption("$expand", "Products").Where(
        //            p => p.ID == 0).First();
        //    Assert.AreEqual(0, food.Products.Count, "The food category should have no product.");
        //}
    }

    [TestFixture]
    public class InMemoryServiceReferenceUpdateTests : ReferenceUpdateTests<ProductInMemoryService>
    {
    }

    [TestFixture]
    public class QueryableServiceReferenceUpdateTests : ReferenceUpdateTests<ProductQueryableService>
    {
    }
}
