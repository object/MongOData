using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Driver;

namespace Mongo.Context
{
    public partial class MongoContext : IDisposable
    {
        protected string connectionString;
        protected MongoServer server;
        protected MongoDatabase database;

        public MongoContext(string connectionString)
        {
            this.connectionString = connectionString;
            this.server = MongoServer.Create(this.connectionString);
            string databaseName = connectionString.Substring(
                connectionString.IndexOf("localhost") + 10,
                connectionString.IndexOf("?") - connectionString.IndexOf("localhost") - 10);
            this.database = server.GetDatabase(databaseName);
        }

        public MongoDatabase Database
        {
            get { return this.database; }
        }

        public void Dispose()
        {
            this.database.Server.Disconnect();
        }

        public void SaveChanges()
        {
        }
    }
}
