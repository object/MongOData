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
}
