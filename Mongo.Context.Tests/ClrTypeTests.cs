

using System;
using System.Linq;
using NUnit.Framework;

namespace Mongo.Context.Tests
{
    public abstract class ClrTypeTests<T> : TestBase<T>
    {
        protected override void PopulateTestData()
        {
            TestData.PopulateWithClrTypes();
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
            Assert.Contains("ClrTypes", tableNames);
        }

        [Test]
        public void SchemaColumnNullability()
        {
            var schema = base.GetSchema();
            base.ValidateColumnNullability(schema);
        }

        [Test]
        public void SchemaColumnNames()
        {
            var schema = base.GetSchema();
            base.ValidatePropertyNames(schema);
        }

        [Test]
        public void VerifyClrTypes()
        {
            var clr = ctx.ClrTypes.All().First();
            Assert.AreEqual(new[] { (byte)1 }, clr.BinaryValue, "The BinaryValue is not correctly filled.");
            Assert.AreEqual(true, clr.BoolValue, "The BoolValue is not correctly filled.");
            Assert.AreEqual(true, clr.NullableBoolValue, "The NullableBoolValue is not correctly filled.");
            Assert.AreEqual(new DateTime(2012, 1, 1), clr.DateTimeValue, "The DateTimeValue is not correctly filled.");
            Assert.AreEqual(new DateTime(2012, 1, 1), clr.NullableDateTimeValue, "The NullableDateTimeValue is not correctly filled.");
            Assert.AreEqual("01:02:03", clr.TimeSpanValue, "The TimeSpan is not correctly filled.");
            Assert.AreEqual("01:02:03", clr.NullableTimeSpanValue, "The NullableTimeSpan is not correctly filled.");
            Assert.AreEqual(Guid.Empty, clr.GuidValue, "The GuidValue is not correctly filled.");
            Assert.AreEqual(Guid.Empty, clr.NullableGuidValue, "The NullableGuidValue is not correctly filled.");
            Assert.AreEqual(1, clr.ByteValue, "The ByteValue is not correctly filled.");
            Assert.AreEqual(1, clr.NullableByteValue, "The NullableByteValue is not correctly filled.");
            Assert.AreEqual(2, clr.SByteValue, "The SByteValue is not correctly filled.");
            Assert.AreEqual(2, clr.NullableSByteValue, "The NullableSByteValue is not correctly filled.");
            Assert.AreEqual(3, clr.Int16Value, "The Int16Value is not correctly filled.");
            Assert.AreEqual(3, clr.NullableInt16Value, "The NullableInt16Value is not correctly filled.");
            Assert.AreEqual(4, clr.UInt16Value, "The UInt16Value is not correctly filled.");
            Assert.AreEqual(4, clr.NullableUInt16Value, "The NullableUInt16Value is not correctly filled.");
            Assert.AreEqual(5, clr.Int32Value, "The Int32Value is not correctly filled.");
            Assert.AreEqual(5, clr.NullableInt32Value, "The NullableInt32Value is not correctly filled.");
            Assert.AreEqual(6, clr.UInt32Value, "The UInt32Value is not correctly filled.");
            Assert.AreEqual(6, clr.NullableUInt32Value, "The NullableUInt32Value is not correctly filled.");
            Assert.AreEqual(7, clr.Int64Value, "The Int64Value is not correctly filled.");
            Assert.AreEqual(7, clr.NullableInt64Value, "The NullableInt64Value is not correctly filled.");
            Assert.AreEqual(8, clr.UInt64Value, "The UInt64Value is not correctly filled.");
            Assert.AreEqual(8, clr.NullableUInt64Value, "The NullableUInt64Value is not correctly filled.");
            Assert.AreEqual(9, clr.SingleValue, "The SingleValue is not correctly filled.");
            Assert.AreEqual(9, clr.NullableSingleValue, "The NullableSingleValue is not correctly filled.");
            Assert.AreEqual(10, clr.DoubleValue, "The DoubleValue is not correctly filled.");
            Assert.AreEqual(10, clr.NullableDoubleValue, "The NullableDoubleValue is not correctly filled.");
            Assert.AreEqual("11", clr.DecimalValue, "The DecimalValue is not correctly filled.");
            Assert.AreEqual("11", clr.NullableDecimalValue, "The NullableDecimalValue is not correctly filled.");
            Assert.AreEqual("abc", clr.StringValue, "The StringValue is not correctly filled.");
        }

        [Test]
        public void QueryClrBoolValue()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.BoolValue == true);
            Assert.IsNotNull(clr);
            Assert.AreEqual(true, clr.BoolValue, "The BoolValue is not correctly filled.");
        }

        [Test]
        public void QueryClrNullableBoolValue()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.NullableBoolValue == true);
            Assert.IsNotNull(clr);
            Assert.AreEqual(true, clr.NullableBoolValue, "The NullableBoolValue is not correctly filled.");
        }

        [Test]
        public void QueryClrDateTimeValue()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.DateTimeValue < DateTime.Now);
            Assert.IsNotNull(clr);
            Assert.AreEqual(new DateTime(2012, 1, 1), clr.DateTimeValue, "The DateTimeValue is not correctly filled.");
        }

        [Test]
        public void QueryClrNullableDateTimeValue()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.NullableDateTimeValue < DateTime.Now);
            Assert.IsNotNull(clr);
            Assert.AreEqual(new DateTime(2012, 1, 1), clr.NullableDateTimeValue, "The NullableDateTimeValue is not correctly filled.");
        }

        [Test]
        public void QueryClrTimeSpanValue()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.TimeSpanValue == "01:02:03");
            Assert.IsNotNull(clr);
            Assert.AreEqual("01:02:03", clr.TimeSpanValue, "The TimeSpanValue is not correctly filled.");
        }

        [Test]
        public void QueryClrNullableTimeSpanValue()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.NullableTimeSpanValue == "01:02:03");
            Assert.IsNotNull(clr);
            Assert.AreEqual("01:02:03", clr.NullableTimeSpanValue, "The NullableTimeSpanValue is not correctly filled.");
        }

        [Test]
        public void QueryClrGuidValue()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.GuidValue == Guid.Empty);
            Assert.IsNotNull(clr);
            Assert.AreEqual(Guid.Empty, clr.GuidValue, "The GuidValueis not correctly filled.");
        }

        [Test]
        public void QueryClrNullableGuidValue()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.NullableGuidValue == Guid.Empty);
            Assert.IsNotNull(clr);
            Assert.AreEqual(Guid.Empty, clr.NullableGuidValue, "The NullableGuidValueis not correctly filled.");
        }

        [Test]
        public void QueryClrByteValue()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.ByteValue == 1);
            Assert.IsNotNull(clr);
            Assert.AreEqual(1, clr.ByteValue, "The ByteValue is not correctly filled.");
        }

        [Test]
        public void QueryClrNullableByteValue()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.NullableByteValue == 1);
            Assert.IsNotNull(clr);
            Assert.AreEqual(1, clr.NullableByteValue, "The NullableByteValue is not correctly filled.");
        }

        [Test]
        public void QueryClrSByteValue()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.SByteValue == 2);
            Assert.IsNotNull(clr);
            Assert.AreEqual(2, clr.SByteValue, "The SByteValue is not correctly filled.");
        }

        [Test]
        public void QueryClrNullableSByteValue()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.NullableSByteValue == 2);
            Assert.IsNotNull(clr);
            Assert.AreEqual(2, clr.NullableSByteValue, "The NullableSByteValue is not correctly filled.");
        }

        [Test]
        public void QueryClrInt16Value()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.Int16Value == 3);
            Assert.IsNotNull(clr);
            Assert.AreEqual(3, clr.Int16Value, "The Int16Value is not correctly filled.");
        }

        [Test]
        public void QueryClrNullableInt16Value()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.NullableInt16Value == 3);
            Assert.IsNotNull(clr);
            Assert.AreEqual(3, clr.NullableInt16Value, "The NullableInt16Value is not correctly filled.");
        }

        [Test]
        public void QueryClrUInt16Value()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.UInt16Value == 4);
            Assert.IsNotNull(clr);
            Assert.AreEqual(4, clr.UInt16Value, "The UInt16Value is not correctly filled.");
        }

        [Test]
        public void QueryClrNullableUInt16Value()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.NullableUInt16Value == 4);
            Assert.IsNotNull(clr);
            Assert.AreEqual(4, clr.NullableUInt16Value, "The NullableUInt16Value is not correctly filled.");
        }

        [Test]
        public void QueryClrInt32Value()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.Int32Value == 5);
            Assert.IsNotNull(clr);
            Assert.AreEqual(5, clr.Int32Value, "The Int32Value is not correctly filled.");
        }

        [Test]
        public void QueryClrNullableInt32Value()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.NullableInt32Value == 5);
            Assert.IsNotNull(clr);
            Assert.AreEqual(5, clr.NullableInt32Value, "The NullableInt32Value is not correctly filled.");
        }

        [Test]
        public void QueryClrUInt32Value()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.UInt32Value == 6);
            Assert.IsNotNull(clr);
            Assert.AreEqual(6, clr.UInt32Value, "The UInt32Value is not correctly filled.");
        }

        [Test]
        public void QueryClrNullableUInt32Value()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.NullableUInt32Value == 6);
            Assert.IsNotNull(clr);
            Assert.AreEqual(6, clr.NullableUInt32Value, "The NullableUInt32Value is not correctly filled.");
        }

        [Test]
        public void QueryClrInt64Value()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.Int64Value == 7);
            Assert.IsNotNull(clr);
            Assert.AreEqual(7, clr.Int64Value, "The Int64Value is not correctly filled.");
        }

        [Test]
        public void QueryClrNullableInt64Value()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.NullableInt64Value == 7);
            Assert.IsNotNull(clr);
            Assert.AreEqual(7, clr.NullableInt64Value, "The NullableInt64Value is not correctly filled.");
        }

        [Test]
        public void QueryClrUInt64Value()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.UInt64Value == 8);
            Assert.IsNotNull(clr);
            Assert.AreEqual(8, clr.UInt64Value, "The UInt64Value is not correctly filled.");
        }

        [Test]
        public void QueryClrNullableUInt64Value()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.NullableUInt64Value == 8);
            Assert.IsNotNull(clr);
            Assert.AreEqual(8, clr.NullableUInt64Value, "The NullableUInt64Value is not correctly filled.");
        }

        [Test]
        public void QueryClrSingleValue()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.SingleValue == 9);
            Assert.IsNotNull(clr);
            Assert.AreEqual(9, clr.SingleValue, "The SingleValue is not correctly filled.");
        }

        [Test]
        public void QueryClrNullableSingleValue()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.NullableSingleValue == 9);
            Assert.IsNotNull(clr);
            Assert.AreEqual(9, clr.NullableSingleValue, "The NullableSingleValue is not correctly filled.");
        }

        [Test]
        public void QueryClrDoubleValue()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.DoubleValue == 10);
            Assert.IsNotNull(clr);
            Assert.AreEqual(10, clr.DoubleValue, "The DoubleValue is not correctly filled.");
        }

        [Test]
        public void QueryClNullableDoubleValue()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.NullableDoubleValue == 10);
            Assert.IsNotNull(clr);
            Assert.AreEqual(10, clr.NullableDoubleValue, "The NullableDoubleValue is not correctly filled.");
        }

        [Test]
        [Ignore("Check WCF DS decimal format")]
        public void QueryClrDecimalValue()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.DecimalValue == 11.1M);
            Assert.IsNotNull(clr);
            Assert.AreEqual(11, clr.DecimalValue, "The DecimalValue is not correctly filled.");
        }

        [Test]
        [Ignore("Check WCF DS decimal format")]
        public void QueryClrNullableDecimalValue()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.NullableDecimalValue == 11M);
            Assert.IsNotNull(clr);
            Assert.AreEqual(11, clr.NullableDecimalValue, "The NullableDecimalValue is not correctly filled.");
        }

        [Test]
        public void QueryClrStringValue()
        {
            var clr = ctx.ClrTypes.Find(ctx.ClrTypes.StringValue == "abc");
            Assert.IsNotNull(clr);
            Assert.AreEqual("abc", clr.StringValue, "The StringValue is not correctly filled.");
        }
    }

    [TestFixture]
    public class InMemoryServiceClrTypeTests : ClrTypeTests<ProductInMemoryService>
    {
    }

    [TestFixture]
    public class QueryableServiceClrTypeTests : ClrTypeTests<ProductQueryableService>
    {
    }
}
