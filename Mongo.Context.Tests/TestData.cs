using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Mongo.Context.Tests
{
    public static class TestData
    {
        public static void PopulateWithCategoriesAndProducts(bool clearDatabase = true)
        {
            var database = GetDatabase(clearDatabase);

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
                        Supplier = new Supplier
                            {
                                Name = "City Bakery",
                                Addresses = new[]
                                    {
                                        new Address { Type = AddressType.Postal, Lines = new[] {"P.O.Box 89", "123456 City"} },
                                        new Address { Type = AddressType.Street, Lines = new[] {"Long Street 100", "654321 City"} },
                                    },
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
                        Supplier = new Supplier
                            {
                                Name = "Green Farm",
                                Addresses = new[]
                                    {
                                        new Address { Type = AddressType.Street, Lines = new[] {"P.O.Box 123", "321321 Green Village"} },
                                    },
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

        public static void PopulateWithVariableTypes(bool clearDatabase = true)
        {
            var database = GetDatabase(clearDatabase);

            var variableTypes = database.GetCollection("VariableTypes");
            variableTypes.Insert(new TypeWithOneField { StringValue = "1" }.ToBsonDocument());
            variableTypes.Insert(new TypeWithTwoFields { StringValue = "2", IntValue = 2 }.ToBsonDocument());
            variableTypes.Insert(new TypeWithThreeFields { StringValue = "3", IntValue = 3, DecimalValue = 3m }.ToBsonDocument());
        }

        public static void PopulateWithBsonIdTypes(bool clearDatabase = true)
        {
            var database = GetDatabase(clearDatabase);

            var typesWithoutExplicitId = database.GetCollection<TypeWithoutExplicitId>("TypeWithoutExplicitId");
            typesWithoutExplicitId.Insert(new TypeWithoutExplicitId { Name = "A" }.ToBsonDocument());
            typesWithoutExplicitId.Insert(new TypeWithoutExplicitId { Name = "B" }.ToBsonDocument());
            typesWithoutExplicitId.Insert(new TypeWithoutExplicitId { Name = "C" }.ToBsonDocument());

            var typeWithBsonId = database.GetCollection<TypeWithBsonId>("TypeWithBsonId");
            typeWithBsonId.Insert(new TypeWithBsonId { Id = ObjectId.GenerateNewId(), Name = "A" }.ToBsonDocument());
            typeWithBsonId.Insert(new TypeWithBsonId { Id = ObjectId.GenerateNewId(), Name = "B" }.ToBsonDocument());
            typeWithBsonId.Insert(new TypeWithBsonId { Id = ObjectId.GenerateNewId(), Name = "C" }.ToBsonDocument());

            var typeWithIntId = database.GetCollection<TypeWithIntId>("TypeWithIntId");
            typeWithIntId.Insert(new TypeWithIntId { Id = 1, Name = "A" }.ToBsonDocument());
            typeWithIntId.Insert(new TypeWithIntId { Id = 2, Name = "B" }.ToBsonDocument());
            typeWithIntId.Insert(new TypeWithIntId { Id = 3, Name = "C" }.ToBsonDocument());

            var typeWithStringId = database.GetCollection<TypeWithStringId>("TypeWithStringId");
            typeWithStringId.Insert(new TypeWithStringId { Id = "1", Name = "A" }.ToBsonDocument());
            typeWithStringId.Insert(new TypeWithStringId { Id = "2", Name = "B" }.ToBsonDocument());
            typeWithStringId.Insert(new TypeWithStringId { Id = "3", Name = "C" }.ToBsonDocument());

            var typeWithGuidId = database.GetCollection<TypeWithGuidId>("TypeWithGuidId");
            typeWithGuidId.Insert(new TypeWithGuidId { Id = Guid.NewGuid(), Name = "A" }.ToBsonDocument());
            typeWithGuidId.Insert(new TypeWithGuidId { Id = Guid.NewGuid(), Name = "B" }.ToBsonDocument());
            typeWithGuidId.Insert(new TypeWithGuidId { Id = Guid.NewGuid(), Name = "C" }.ToBsonDocument());
        }

        public static void PopulateWithJsonSamples(bool clearDatabase = true)
        {
            var database = GetDatabase(clearDatabase);

            var jsonSamples = new[] { "Colors", "Facebook", "Flickr", "GoogleMaps", "iPhone", "Twitter", "YouTube", "Nested", "ArrayOfNested" };

            foreach (var collectionName in jsonSamples)
            {
                var json = GetResourceAsString(collectionName + ".json");
                var doc = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(json);
                var collection = database.GetCollection(collectionName);
                collection.Insert(doc);
            }
        }

        public static void Clean()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString;
            var server = new MongoClient(connectionString).GetServer();

            server.DropDatabase(GetDatabaseName(connectionString));
        }

        public static MongoDatabase CreateDatabase()
        {
            return GetDatabase(true);
        }

        public static MongoDatabase OpenDatabase()
        {
            return GetDatabase(false);
        }

        private static MongoDatabase GetDatabase(bool clear)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString;
            var databaseName = GetDatabaseName(connectionString);
            var server = new MongoClient(connectionString).GetServer();
            if (clear)
                server.DropDatabase(databaseName);
            return server.GetDatabase(databaseName);
        }

        private static string GetDatabaseName(string connectionString)
        {
            string databaseName = connectionString.Substring(connectionString.IndexOf("localhost") + 10);
            int optionsIndex = databaseName.IndexOf("?");
            if (optionsIndex > 0)
            {
                databaseName = databaseName.Substring(0, optionsIndex);
            }
            return databaseName;
        }

        private static string GetResourceAsString(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var completeResourceName = assembly.GetManifestResourceNames().Single(o => o.EndsWith("." + resourceName));
            using (Stream resourceStream = assembly.GetManifestResourceStream(completeResourceName))
            {
                TextReader reader = new StreamReader(resourceStream);
                string result = reader.ReadToEnd();
                return result;
            }
        }
    }
}
