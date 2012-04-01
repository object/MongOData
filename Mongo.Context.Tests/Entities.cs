using System;
using System.Collections.Generic;
using System.Data.Services.Common;
using System.Linq;
using System.Text;
using MongoDB.Bson;

namespace Mongo.Context.Tests
{
    public class Quantity
    {
        public double Value { get; set; }
        public string Units { get; set; }
    }

    public class ClientProduct
    {
        public ObjectId _id { get; set; }

        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime ReleaseDate { get; set; }
        public DateTime? DiscontinueDate { get; set; }
        public int Rating { get; set; }
        public Quantity Quantity { get; set; }
        public ClientCategory Category { get; set; }
    }

    public class ClientCategory
    {
        public ObjectId _id { get; set; }

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
        public ObjectId _id { get; set; }

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
}
