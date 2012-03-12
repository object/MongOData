using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using MongoDB.Driver;
using NUnit.Framework;
using Simple.Data;
using Simple.Data.OData;

namespace Mongo.Context.Tests
{
    public abstract class QueryTests<T>
    {
        protected TestService service;
        protected dynamic ctx;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            TestData.Populate();
            service = new TestService(typeof(T));
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            if (service != null)
            {
                service.Dispose();
                service = null;
            }

            TestData.Clean();
        }

        [SetUp]
        public void SetUp()
        {
            ctx = Database.Opener.Open(service.ServiceUri);
        }

        [Test]
        public void Metadata()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(service.ServiceUri + "/$metadata");
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "The $metadata didn't return success.");
        }

        [Test]
        public void AllEntitiesVerifyResultCount()
        {
            var q = ctx.Products.All().ToList();
            Assert.AreEqual(3, q.Count, "The service returned unexpected number of results.");
        }

        [Test]
        public void AllEntitiesTakeOneVerifyResultCount()
        {
            var q = ctx.Products.All().Take(1).ToList();
            Assert.AreEqual(1, q.Count, "The service returned unexpected number of results.");
        }

        [Test]
        public void AllEntitiesSkipOneVerifyResultCount()
        {
            var q = ctx.Products.All().Skip(1).ToList();
            Assert.AreEqual(2, q.Count, "The service returned unexpected number of results.");
        }

        [Test]
        public void AllEntitiesCountVerifyResult()
        {
            var q = ctx.Products.All().Count();
            Assert.AreEqual(3, q, "The count is not correctly computed.");
        }

        [Test]
        public void AllEntitiesVerifyID()
        {
            var q = ctx.Products.All().ToList();
            Assert.AreEqual(3, q[2].ID, "The ID is not correctly filled.");
        }

        [Test]
        public void AllEntitiesVerifyName()
        {
            var q = ctx.Products.All().ToList();
            Assert.AreEqual("Milk", q[1].Name, "The Name is not correctly filled.");
        }

        [Test]
        public void AllEntitiesOrderby()
        {
            var q = ctx.Products.All().OrderBy(ctx.Products.Name).ToList();
            for (int i = 0; i < 2; i++)
            {
                Assert.Greater(q[i+1].Name, q[i].Name, "Names are not in correct order");
            }
        }

        [Test]
        public void AllEntitiesOrderbyDescending()
        {
            var q = ctx.Products.All().OrderByDescending(ctx.Products.Name).ToList();
            for (int i = 0; i < 2; i++)
            {
                Assert.Less(q[i + 1].Name, q[i].Name, "Names are not in correct order");
            }
        }

        [Test]
        public void AllEntitiesVerifyQuantityValue()
        {
            var q = ctx.Products.All().ToList();
            Assert.AreEqual(12, q[0].Quantity.Value, "Unexpected quantity value.");
        }

        [Test]
        public void FilterEqualID()
        {
            Assert.AreEqual(1, ctx.Products.FindAll(ctx.Products.ID == 1).Count());
        }

        [Test]
        public void FilterGreaterID()
        {
            Assert.AreEqual(3, ctx.Products.FindAll(ctx.Products.ID > 0).Count());
        }

        [Test]
        public void FilterNameLength()
        {
            Assert.AreEqual(2, ctx.Products.FindAll(ctx.Products.Name.Length() == 4).Count());
        }

        [Test]
        public void FilterNameContains()
        {
            Assert.AreEqual(2, ctx.Products.FindAll(ctx.Products.Name.Contains("i") == true).Count());
        }

        [Test]
        public void FilterGreaterRating()
        {
            Assert.AreEqual(2, ctx.Products.FindAll(ctx.Products.Rating > 3).Count());
        }

        [Test]
        public void FilterGreaterIDAndNameLength()
        {
            Assert.AreEqual(2, ctx.Products.FindAll(ctx.Products.ID > 0 && ctx.Products.Name.Length() == 4).Count());
        }

        //[Test]
        //public void Projections()
        //{
        //    VerifySelectedProperties<ClientProduct>("/Products?$select=ID&$filter=ID gt 0", "ID");
        //    VerifySelectedProperties<ClientProduct>("/Products?$select=Name", "Name");
        //    VerifySelectedProperties<ClientProduct>("/Products?$select=Quantity", "Quantity");
        //    VerifySelectedProperties<ClientProduct>("/Products?$select=Name,Description,Rating", "Name", "Description", "Rating");
        //    VerifySelectedProperties<ClientProduct>("/Products?$select=*&$filter=ID eq 2", "ID", "Name", "Description", "ReleaseDate", "DiscontinueDate", "Rating", "Quantity");
        //}

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
    public class InMemoryServiceQueryTests : QueryTests<ProductInMemoryService>
    {
    }

    [TestFixture]
    public class QueryableServiceQueryTests : QueryTests<ProductQueryableService>
    {
    }
}
