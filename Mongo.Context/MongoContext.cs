

using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Bson;

namespace Mongo.Context
{
    public partial class MongoContext
    {
        public IMongoDatabase Database { get; private set; }

        protected string _connectionString;
        protected MongoClient _client;

        public MongoContext(string connectionString)
        {
            _connectionString = connectionString;
            string databaseName = GetDatabaseName(_connectionString);

            _client = new MongoClient(_connectionString);
            Database = _client.GetDatabase(databaseName);
        }


        public static IEnumerable<string> GetDatabaseNames(string connectionString)
        {
            var mongoClient = new MongoClient(connectionString);
            var databaseNamesCursor = mongoClient.ListDatabasesAsync().GetAwaiter().GetResult();
            var databases = databaseNamesCursor.ToListAsync().GetAwaiter().GetResult();
            var databaseNames = databases.Select(x => x["name"].AsString);
            return databaseNames;
        }

        public void SaveChanges()
        {
        }

        private string GetDatabaseName(string connectionString)
        {
            var hostIndex = connectionString.IndexOf("//");
            if (hostIndex > 0)
            {
                int startIndex = connectionString.IndexOf("/", hostIndex + 2) + 1;
                int endIndex = connectionString.IndexOf("?", startIndex);
                if (startIndex > 0)
                {
                    if (endIndex > 0)
                        return connectionString.Substring(startIndex, endIndex - startIndex);
                    else
                        return connectionString.Substring(startIndex);
                }
            }

            throw new ArgumentException("Unsupported MongoDB connection string", "connectionString");
        }
    }
}
