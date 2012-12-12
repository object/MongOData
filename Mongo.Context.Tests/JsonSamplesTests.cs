using System;
using System.Data.Services.Client;
using System.Linq;
using System.Net;
using Microsoft.CSharp.RuntimeBinder;
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
        public void ValidateMetadata()
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
            Assert.AreEqual(18, result.menu.items.Count);
            Assert.AreEqual("Open", result.menu.items.First().id);
            Assert.Throws<RuntimeBinderException>(() => { var x = result.menu.items.First().label; });
            Assert.AreEqual("About", result.menu.items.Last().id);
            // The next line will work when dynamic schema update is available
            //Assert.AreEqual("About xProgress CVG Viewer...", result.menu.items.Last().label);
        }

        [Test]
        public void Twitter()
        {
            var result = ctx.Twitter.All().First();
            Assert.AreEqual("@twitterapi  http://tinyurl.com/ctrefg", result.results.text);
            Assert.AreEqual("popular", result.results.metadata.result_type);
            Assert.AreEqual(109, result.results.metadata.recent_retweets);
        }

        [Test]
        public void YouTube()
        {
            var result = ctx.YouTube.All().First();
            Assert.AreEqual(@"Talk On Travel Pool", result.title);
            Assert.AreEqual(1, result.items.Count);
            Assert.AreEqual("View from the hotel", result.items.First().title);
        }

        [Test]
        public void Nested()
        {
            var result = ctx.Nested.All().First();
            Assert.AreEqual("0001", result.id);
            Assert.AreEqual(4, result.batters.batter.Count);
            Assert.AreEqual("1001", result.batters.batter.First().id);
            Assert.AreEqual(7, result.topping.Count);
            Assert.AreEqual("5001", result.topping.First().id);
        }

        [Test]
        public void ArrayOfNested()
        {
            var result = ctx.ArrayOfNested.All().First();
            Assert.AreEqual("0001", result.nestedArray.First().id);
            Assert.AreEqual(4, result.nestedArray.First().batters.batter.Count);
            Assert.AreEqual("1001", result.nestedArray.First().batters.batter.First().id);
            Assert.AreEqual(7, result.nestedArray.First().topping.Count);
            Assert.AreEqual("5001", result.nestedArray.First().topping.First().id);
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
