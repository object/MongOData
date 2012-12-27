using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CreateSampleData
{
    public class Database
    {
        private MongoDatabase mongoDatabase;
        private int protocolVersion;

        public static Database Create(int protocolVersion, string connectionString)
        {
            var databaseName = GetDatabaseName(connectionString);
            var server = new MongoClient(connectionString).GetServer();
            server.DropDatabase(databaseName);
            var database = new Database();
            database.mongoDatabase = server.GetDatabase(databaseName);
            database.protocolVersion = protocolVersion;
            return database;
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

        public void PopulateWithCategoriesAndProducts()
        {
            var categoryCollection = this.mongoDatabase.GetCollection<Category>("Categories");
            var categories = CreateCaterories();
            categories.ForEach(x => categoryCollection.Insert(x));

            var productCollection = this.mongoDatabase.GetCollection<Product>("Products");
            var products = CreateProducts(categories);
            if (this.protocolVersion < 3)
            {
                products.ForEach(x => x.Supplier = null);
            }
            products.ToList().ForEach(x => productCollection.Insert(x));
        }

        private List<Category> CreateCaterories()
        {
            return new List<Category>()
                {
                    new Category
                        {
                            ID = 1,
                            Name = "Food",
                            Products = null,
                        },
                    new Category
                        {
                            ID = 2,
                            Name = "Beverages",
                            Products = null,
                        },
                    new Category
                        {
                            ID = 3,
                            Name = "Electronics",
                            Products = null,
                        },
                };
        }

        private List<Product> CreateProducts(IList<Category> categories)
        {
            return new List<Product>()
                {
                    new Product
                        {
                            ID = 1,
                            Name = "Bread",
                            Description = "Whole grain bread",
                            ReleaseDate = new DateTime(1992, 1, 1),
                            DiscontinueDate = null,
                            Rating = 4,
                            Quantity = new Quantity
                                {
                                    Value = (double) 12,
                                    Units = "pieces",
                                },
                            Supplier = new Supplier
                                {
                                    Name = "City Bakery",
                                    Addresses = new[]
                                        {
                                            new Address
                                                {
                                                    Type = AddressType.Postal,
                                                    Lines = new[] {"P.O.Box 89", "123456 City"}
                                                },
                                            new Address
                                                {
                                                    Type = AddressType.Street,
                                                    Lines = new[] {"Long Street 100", "654321 City"}
                                                },
                                        },
                                },
                            Category = categories[0],
                        },
                    new Product
                        {
                            ID = 2,
                            Name = "Milk",
                            Description = "Low fat milk",
                            ReleaseDate = new DateTime(1995, 10, 21),
                            DiscontinueDate = null,
                            Rating = 3,
                            Quantity = new Quantity
                                {
                                    Value = (double) 4,
                                    Units = "liters",
                                },
                            Supplier = new Supplier
                                {
                                    Name = "Green Farm",
                                    Addresses = new[]
                                        {
                                            new Address
                                                {
                                                    Type = AddressType.Street,
                                                    Lines = new[] {"P.O.Box 123", "321321 Green Village"}
                                                },
                                        },
                                },
                            Category = categories[1],
                        },
                    new Product
                        {
                            ID = 3,
                            Name = "Wine",
                            Description = "Red wine, year 2003",
                            ReleaseDate = new DateTime(2003, 11, 24),
                            DiscontinueDate = new DateTime(2008, 3, 1),
                            Rating = 5,
                            Quantity = new Quantity
                                {
                                    Value = (double) 7,
                                    Units = "bottles",
                                },
                            Category = categories[1],
                        },
                };
        }

        public void PopulateWithJsonSamples()
        {
            var jsonSamples = new[] { "Colors", "Facebook", "Flickr", "GoogleMaps", "iPhone", "Twitter", "YouTube", "Nested", "ArrayOfNested", "EmptyArray", "NullArray" };

            foreach (var collectionName in jsonSamples)
            {
                var jsonCollection = GetResourceAsString(collectionName + ".json").Split(new string[] { "---" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var json in jsonCollection)
                {
                    var doc = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(json);
                    var collection = this.mongoDatabase.GetCollection(collectionName);
                    collection.Insert(doc);
                }
            }
        }

        private string GetResourceAsString(string resourceName)
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