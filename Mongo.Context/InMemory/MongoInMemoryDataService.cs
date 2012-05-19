using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataServiceProvider;

namespace Mongo.Context.InMemory
{
    public abstract class MongoInMemoryDataService : DSPDataService<DSPInMemoryContext, DSPResourceQueryProvider, DSPUpdateProvider>
    {
        protected string connectionString;
        protected static Action<string> ResetDataContext;
        private static DSPInMemoryContext context;

        /// <summary>Constructor</summary>
        public MongoInMemoryDataService(string connectionString)
        {
            this.connectionString = connectionString;
            this.createUpdateProvider = () => new MongoDSPUpdateProvider(this.connectionString, this.CurrentDataSource, this.metadata);

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
                    this.metadata = new MongoMetadata(this.connectionString).Metadata;
                }
            }
            return metadata;
        }
    }
}
