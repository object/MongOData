using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Mongo.Context.Tests
{
    public static class TestData
    {
        public static async Task PopulateWithCategoriesAndProductsAsync(bool clearDatabase = true)
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

            await categories.InsertOneAsync(categoryFood);
            await categories.InsertOneAsync(categoryBeverages);
            await categories.InsertOneAsync(categoryElectronics);

            await products.InsertOneAsync(
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
            await products.InsertOneAsync(
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
            await products.InsertOneAsync(
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
        }

        public static async Task PopulateWithClrTypesAsync(bool clearDatabase = true)
        {
            var database = GetDatabase(clearDatabase);

            var clrTypes = database.GetCollection<ClrType>("ClrTypes");
            await clrTypes.InsertOneAsync(
                new ClrType
                {
                    BinaryValue = new[] { (byte)1 },
                    BoolValue = true,
                    NullableBoolValue = true,
                    DateTimeValue = new DateTime(2012, 1, 1),
                    NullableDateTimeValue = new DateTime(2012, 1, 1),
                    TimeSpanValue = new TimeSpan(1, 2, 3),
                    NullableTimeSpanValue = new TimeSpan(1, 2, 3),
                    GuidValue = Guid.Empty,
                    NullableGuidValue = Guid.Empty,
                    ByteValue = (byte)1,
                    NullableByteValue = (byte)1,
                    SByteValue = (sbyte)2,
                    NullableSByteValue = (sbyte)2,
                    Int16Value = 3,
                    NullableInt16Value = 3,
                    UInt16Value = 4,
                    NullableUInt16Value = 4,
                    Int32Value = 5,
                    NullableInt32Value = 5,
                    UInt32Value = 6,
                    NullableUInt32Value = 6,
                    Int64Value = 7,
                    NullableInt64Value = 7,
                    UInt64Value = 8,
                    NullableUInt64Value = 8,
                    SingleValue = 9,
                    NullableSingleValue = 9,
                    DoubleValue = 10,
                    NullableDoubleValue = 10,
                    DecimalValue = 11,
                    NullableDecimalValue = 11,
                    StringValue = "abc",
                    ObjectIdValue = new BsonObjectId(new ObjectId(100, 200, 300, 400)),
                });
        }

        public static async Task PopulateWithVariableTypesAsync(bool clearDatabase = true)
        {
            var database = GetDatabase(clearDatabase);

            var variableTypes = database.GetCollection<BsonDocument>("VariableTypes");
            await variableTypes.InsertOneAsync(new TypeWithOneField { StringValue = "1" }.ToBsonDocument());
            await variableTypes.InsertOneAsync(new TypeWithTwoFields { StringValue = "2", IntValue = 2 }.ToBsonDocument());
            await variableTypes.InsertOneAsync(new TypeWithThreeFields { StringValue = "3", IntValue = 3, DecimalValue = 3m }.ToBsonDocument());
        }

        public static async Task PopulateWithBsonIdTypesAsync(bool clearDatabase = true)
        {
            var database = GetDatabase(clearDatabase);

            var typesWithoutExplicitId = database.GetCollection<TypeWithoutExplicitId>("TypeWithoutExplicitId");
            await typesWithoutExplicitId.InsertOneAsync(new TypeWithoutExplicitId { Name = "A" });
            await typesWithoutExplicitId.InsertOneAsync(new TypeWithoutExplicitId { Name = "B" });
            await typesWithoutExplicitId.InsertOneAsync(new TypeWithoutExplicitId { Name = "C" });

            var typeWithBsonId = database.GetCollection<TypeWithBsonId>("TypeWithBsonId");
            await typeWithBsonId.InsertOneAsync(new TypeWithBsonId { Id = ObjectId.GenerateNewId(), Name = "A" });
            await typeWithBsonId.InsertOneAsync(new TypeWithBsonId { Id = ObjectId.GenerateNewId(), Name = "B" });
            await typeWithBsonId.InsertOneAsync(new TypeWithBsonId { Id = ObjectId.GenerateNewId(), Name = "C" });

            var typeWithIntId = database.GetCollection<TypeWithIntId>("TypeWithIntId");
            await typeWithIntId.InsertOneAsync(new TypeWithIntId { Id = 1, Name = "A" });
            await typeWithIntId.InsertOneAsync(new TypeWithIntId { Id = 2, Name = "B" });
            await typeWithIntId.InsertOneAsync(new TypeWithIntId { Id = 3, Name = "C" });

            var typeWithStringId = database.GetCollection<TypeWithStringId>("TypeWithStringId");
            await typeWithStringId.InsertOneAsync(new TypeWithStringId { Id = "1", Name = "A" });
            await typeWithStringId.InsertOneAsync(new TypeWithStringId { Id = "2", Name = "B" });
            await typeWithStringId.InsertOneAsync(new TypeWithStringId { Id = "3", Name = "C" });

            var typeWithGuidId = database.GetCollection<TypeWithGuidId>("TypeWithGuidId");
            await typeWithGuidId.InsertOneAsync(new TypeWithGuidId { Id = Guid.NewGuid(), Name = "A" });
            await typeWithGuidId.InsertOneAsync(new TypeWithGuidId { Id = Guid.NewGuid(), Name = "B" });
            await typeWithGuidId.InsertOneAsync(new TypeWithGuidId { Id = Guid.NewGuid(), Name = "C" });
        }

        public static async Task PopulateWithJsonSamplesAsync(bool clearDatabase = true)
        {
            var database = GetDatabase(clearDatabase);

            var jsonSamples = new[]
                {
                    "Colors",
                    "Facebook",
                    "Flickr",
                    "GoogleMaps",
                    "iPhone",
                    "Twitter",
                    "YouTube",
                    "Nested",
                    "ArrayOfNested",
                    "ArrayInArray",
                    "EmptyArray",
                    "NullArray",
                    "UnresolvedArray",
                    "UnresolvedProperty",
                    "EmptyProperty",
                };

            foreach (var collectionName in jsonSamples)
            {
                var jsonCollection = GetResourceAsString(collectionName + ".json").Split(new string[] { "---" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var json in jsonCollection)
                {
                    var doc = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(json);
                    var collection = database.GetCollection<BsonDocument>(collectionName);
                    await collection.InsertOneAsync(doc);
                }
            }
        }

        public static Task CleanAsync()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString;
            var client = new MongoClient(connectionString);
            return client.DropDatabaseAsync(GetDatabaseName(connectionString));
        }

        public static IMongoDatabase CreateDatabase()
        {
            return GetDatabase(true);
        }

        public static IMongoDatabase OpenDatabase()
        {
            return GetDatabase(false);
        }

        private static IMongoDatabase GetDatabase(bool clear)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString;
            var databaseName = GetDatabaseName(connectionString);
            var client = new MongoClient(connectionString);
            if (clear)
                client.DropDatabaseAsync(databaseName).Wait();
            return client.GetDatabase(databaseName);
        }

        private static string GetDatabaseName(string connectionString)
        {
            string databaseName = connectionString.Substring(connectionString.LastIndexOf("/") + 1);
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
