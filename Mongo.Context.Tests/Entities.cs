using System;
using System.Collections.Generic;
using System.Data.Services.Common;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Mongo.Context.Tests
{
    public class Quantity
    {
        public double Value { get; set; }
        public string Units { get; set; }
    }

    public class Supplier
    {
        public string Name { get; set; }
    }

    public class ClientProduct
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime ReleaseDate { get; set; }
        public DateTime? DiscontinueDate { get; set; }
        public int Rating { get; set; }
        public Quantity Quantity { get; set; }
        public Supplier Supplier { get; set; }
        public ClientCategory Category { get; set; }
    }

    public class ClientCategory
    {
        public ClientCategory()
        {
            this.Products = new List<ClientProduct>();
        }

        public int ID { get; set; }
        public string Name { get; set; }
        public List<ClientProduct> Products { get; set; }
    }

    public class ClrType
    {
        public byte[] BinaryValue { get; set; }
        public bool BoolValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        public TimeSpan TimeSpanValue { get; set; }
        public Guid GuidValue { get; set; }
        public Byte ByteValue { get; set; }
        public SByte SByteValue { get; set; }
        public Int16 Int16Value { get; set; }
        public UInt16 UInt16Value { get; set; }
        public Int32 Int32Value { get; set; }
        public UInt32 UInt32Value { get; set; }
        public Int64 Int64Value { get; set; }
        public UInt64 UInt64Value { get; set; }
        public Single SingleValue { get; set; }
        public Double DoubleValue { get; set; }
        public Decimal DecimalValue { get; set; }
        public String StringValue { get; set; }
    }

    public class TypeWithoutExplicitId
    {
        public string Name { get; set; }
    }

    public class TypeWithBsonId
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string Name { get; set; }
    }

    public class TypeWithIntId
    {
        [BsonId]
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class TypeWithStringId
    {
        [BsonId]
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class TypeWithGuidId
    {
        [BsonId]
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public class EmptyType
    {
    }

    public class TypeWithOneField : EmptyType
    {
        public string StringValue { get; set; }
    }

    public class TypeWithTwoFields : TypeWithOneField
    {
        public int IntValue { get; set; }
    }

    public class TypeWithThreeFields : TypeWithTwoFields
    {
        public decimal DecimalValue { get; set; }
    }
}
