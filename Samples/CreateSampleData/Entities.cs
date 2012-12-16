using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CreateSampleData
{
    public class Quantity
    {
        public double Value { get; set; }
        public string Units { get; set; }
    }

    public class Supplier
    {
        public string Name { get; set; }
        public Address[] Addresses { get; set; }
    }

    public enum AddressType
    {
        Postal,
        Street,
    }

    public class Address
    {
        public AddressType Type { get; set; }
        public string[] Lines { get; set; }
    }

    public class Product
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime ReleaseDate { get; set; }
        public DateTime? DiscontinueDate { get; set; }
        public int Rating { get; set; }
        public Quantity Quantity { get; set; }
        public Category Category { get; set; }
        public Supplier Supplier { get; set; }
    }

    public class Category
    {
        public Category()
        {
            this.Products = new List<Product>();
        }

        public int ID { get; set; }
        public string Name { get; set; }
        public List<Product> Products { get; set; }
    }
}
