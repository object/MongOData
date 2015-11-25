using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Threading.Tasks;

namespace Mongo.Context
{
    public partial class MongoContext : IDisposable
    {
        protected string connectionString;
        protected MongoClient client;
        //protected MongoServer server;
        //protected MongoDatabase database;

        public MongoContext(string connectionString)
        {
            this.connectionString = connectionString;
            string databaseName = GetDatabaseName(this.connectionString);

            this.client = new MongoClient(this.connectionString);
            //this.server = this.client.GetServer();
            //this.database = server.GetDatabase(databaseName);
            Database = client.GetDatabase(databaseName);
            var col=Database.GetCollection<BsonDocument>("xx");
        }

        public IMongoDatabase Database{ get; private set; }
        //public MongoDatabase Database
        //{
        //    get { return this.database; }
        //}

        public static IEnumerable<string> GetDatabaseNames(string connectionString)
        {
            var mongoClient = new MongoClient(connectionString);
            var databaseNamesCursor = mongoClient.ListDatabasesAsync().GetAwaiter().GetResult();
            var databases = databaseNamesCursor.ToListAsync().GetAwaiter().GetResult();
            var databaseNames = databases.Select(x => x["name"].AsString);
            return databaseNames;
        }

        public void Dispose()
        {
            //this.database.Server.Disconnect();
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
