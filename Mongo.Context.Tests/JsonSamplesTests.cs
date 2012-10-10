using System;
using System.Data.Services.Client;
using System.Linq;
using System.Net;
using NUnit.Framework;
using Simple.Data;
using Simple.Data.OData;

namespace Mongo.Context.Tests
{
    public abstract class JsonSamplesTests<T> : TestBase<T>
    {
        protected override void PopulateTestData()
        {
            TestData.PopulateWithJsonSamples();
        }

        [Test]
        public void Metadata()
        {
            base.RequestAndValidateMetadata();
        }

        [Test]
        public void Colors()
        {
            var result = ctx.Colors.All().First();
            Assert.AreEqual(7, result.colorsArray.Count);
            Assert.AreEqual("red", result.colorsArray[0].colorName);
            Assert.AreEqual("#f00", result.colorsArray[0].hexValue);
            Assert.AreEqual("black", result.colorsArray[6].colorName);
            Assert.AreEqual("#000", result.colorsArray[6].hexValue);
        }

        [Test]
        public void Facebook()
        {
            var result = ctx.Facebook.All().First();
            Assert.AreEqual(2, result.data.Count);
            Assert.AreEqual("Tom Brady", result.data[0].from.name);
            Assert.AreEqual("X12", result.data[0].from.id);
            Assert.AreEqual(2, result.data[0].actions.Count);
            Assert.AreEqual("Comment", result.data[0].actions[0].name);
            Assert.AreEqual("Like", result.data[0].actions[1].name);
        }

        [Test]
        public void Flickr()
        {
            var result = ctx.Flickr.All().First();
            Assert.AreEqual("Talk On Travel Pool", result.title);
            Assert.AreEqual(1, result.items.Count);
            Assert.AreEqual("spain dolphins tenerife canaries lagomera aqualand playadelasamericas junglepark losgigantos loscristines talkontravel", result.items.First().tags);
        }

        [Test]
        public void GoogleMaps()
        {
            var result = ctx.GoogleMaps.All().First();
            Assert.AreEqual(3, result.markers.Count);
            Assert.AreEqual(40.266044, result.markers.First().point.latitude);
            Assert.AreEqual(-74.718479, result.markers.First().point.longitude);
            Assert.AreEqual("Linux users group meets second Wednesday of each month.", result.markers.First().information);
            Assert.AreEqual("", result.markers.First().capacity);
        }

        [Test]
        public void iPhone()
        {
            var result = ctx.iPhone.All().First();
        }

        [Test]
        public void Twitter()
        {
            var result = ctx.Twitter.All().First();
        }

        [Test]
        public void YouTube()
        {
            var result = ctx.YouTube.All().First();
        }

        [Test]
        public void Nested()
        {
            var result = ctx.Nested.All().First();
        }

        [Test]
        public void ArrayOfNested()
        {
            var result = ctx.ArrayOfNested.All().First();
        }
    }

    [TestFixture]
    public class InMemoryServiceJsonSamplesTests : JsonSamplesTests<ProductInMemoryService>
    {
    }

    [TestFixture]
    public class QueryableServiceJsonSamplesTests : JsonSamplesTests<ProductQueryableService>
    {
    }
}
