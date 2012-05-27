using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Mongo.Context.Tests;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver;

namespace Mongo.Context.Tests
{
    public static class TestData
    {
        public static void PopulateWithCategoriesAndProducts()
        {
            var database = CreateDatabase();

            var categories = database.GetCollection<ClientCategory>("Categories");
            var products = database.GetCollection<ClientProduct>("Products");

            var categoryFood = new ClientCategory
                                   {
                                       Name = "Food",
                                       Products = null,
                                   };
            var categoryBeverages = new ClientCategory
                                        {
                                            Name = "Beverages",
                                            Products = null,
                                        };
            var categoryElectronics = new ClientCategory
                                          {
                                              Name = "Electronics",
                                              Products = null,
                                          };

            categories.Insert(categoryFood);
            categories.Insert(categoryBeverages);
            categories.Insert(categoryElectronics);

            products.Insert(
                new ClientProduct
                    {
                        ID = 1,
                        Name = "Bread",
                        Description = "Whole grain bread",
                        ReleaseDate = new DateTime(1992, 1, 1),
                        DiscontinueDate = null,
                        Rating = 4,
                        Quantity = new Quantity
                                       {
                                           Value = (double)12,
                                           Units = "pieces",
                                       },
                        Category = categoryFood,
                    });
            products.Insert(
                new ClientProduct
                    {
                        ID = 2,
                        Name = "Milk",
                        Description = "Low fat milk",
                        ReleaseDate = new DateTime(1995, 10, 21),
                        DiscontinueDate = null,
                        Rating = 3,
                        Quantity = new Quantity
                                       {
                                           Value = (double)4,
                                           Units = "liters",
                                       },
                        Category = categoryBeverages,
                    });
            products.Insert(
                new ClientProduct
                    {
                        ID = 3,
                        Name = "Wine",
                        Description = "Red wine, year 2003",
                        ReleaseDate = new DateTime(2003, 11, 24),
                        DiscontinueDate = new DateTime(2008, 3, 1),
                        Rating = 5,
                        Quantity = new Quantity
                                       {
                                           Value = (double)7,
                                           Units = "bottles",
                                       },
                        Category = categoryBeverages,
                    });

            var clrTypes = database.GetCollection<ClrType>("ClrTypes");
            clrTypes.Insert(
                new ClrType
                    {
                        BinaryValue = new[] { (byte)1 },
                        BoolValue = true,
                        DateTimeValue = new DateTime(2012, 1, 1),
                        TimeSpanValue = new TimeSpan(1, 2, 3),
                        GuidValue = Guid.Empty,
                        ByteValue = (byte)1,
                        SByteValue = (sbyte)2,
                        Int16Value = 3,
                        UInt16Value = 4,
                        Int32Value = 5,
                        UInt32Value = 6,
                        Int64Value = 7,
                        UInt64Value = 8,
                        SingleValue = 9,
                        DoubleValue = 10,
                        DecimalValue = 11,
                        StringValue = "abc",
                    });
        }

        public static void PopulateWithVariableTypes()
        {
            var database = CreateDatabase();

            var variableTypes = database.GetCollection("VariableTypes");
            variableTypes.Insert(new TypeWithOneField { StringValue = "1" }.ToBsonDocument());
            variableTypes.Insert(new TypeWithTwoFields { StringValue = "2", IntValue = 2 }.ToBsonDocument());
            variableTypes.Insert(new TypeWithThreeFields { StringValue = "3", IntValue = 3, DecimalValue = 3m }.ToBsonDocument());
        }

        public static void Clean()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString;
            var server = MongoServer.Create(connectionString);

            server.DropDatabase(GetDatabaseName(connectionString));
        }

        private static MongoDatabase CreateDatabase()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString;
            var server = MongoServer.Create(connectionString);
            var databaseName = GetDatabaseName(connectionString);
            server.DropDatabase(databaseName);
            return server.GetDatabase(databaseName);
        }

        private static string GetDatabaseName(string connectionString)
        {
            return connectionString.Substring(
                connectionString.IndexOf("localhost") + 10,
                connectionString.IndexOf("?") - connectionString.IndexOf("localhost") - 10);
        }
    }
}
