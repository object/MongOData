using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Data.Services;
using System.Data.Services.Providers;
using DataServiceProvider;
using MongoDB.Bson;
using Mongo.Context;

namespace Mongo.Context.InMemory
{
    public abstract class MongoInMemoryDataService : DSPDataService<DSPInMemoryContext, DSPResourceQueryProvider>
    {
        protected string connectionString;
        protected static Action<string> ResetDataContext;
        private static DSPInMemoryContext context;

        /// <summary>Constructor</summary>
        public MongoInMemoryDataService(string connectionString)
        {
            this.connectionString = connectionString;

            ResetDataContext = x => MongoInMemoryDataService.context = new MongoInMemoryContext().CreateContext(base.Metadata, x);
            ResetDataContext(connectionString);
        }

        public static IDisposable RestoreDataContext(string connectionString)
        {
            return new RestoreDataContextDisposable(connectionString);
        }

        private class RestoreDataContextDisposable : IDisposable
        {
            private string connectionString;

            public RestoreDataContextDisposable(string connectionString)
            {
                this.connectionString = connectionString;
            }

            public void Dispose()
            {
                ResetDataContext(this.connectionString);
            }
        }

        protected override DSPInMemoryContext CreateDataSource()
        {
            return context;
        }

        protected override DSPMetadata CreateDSPMetadata()
        {
            lock(this)
            {
                if (this.metadata == null)
                {
                    this.metadata = new MongoMetadata().CreateMetadata(this.connectionString);
                }
            }
            return metadata;
        }
    }
}
