using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataServiceProvider;

namespace Mongo.Context.Queryable
{
    public abstract class MongoQueryableDataService : DSPDataService<DSPQueryableContext, MongoDSPResourceQueryProvider, MongoDSPUpdateProvider>
    {
        protected string connectionString;
        protected static Action<string> ResetDataContext;
        private static DSPQueryableContext context;
        private static new DSPMetadata metadata;

        public MongoQueryableDataService(string connectionString)
        {
            this.connectionString = connectionString;
            this.createResourceQueryProvider = () => new MongoDSPResourceQueryProvider();
            this.createUpdateProvider = () => new MongoDSPUpdateProvider(this.connectionString, this.CurrentDataSource, Metadata);

            ResetDataContext = x =>
                                   {
                                       var mongoMetadata = new MongoMetadata(x);
                                       MongoQueryableDataService.metadata = mongoMetadata.Metadata;
                                       MongoQueryableDataService.context = new MongoQueryableContext().CreateContext(metadata, mongoMetadata.ProviderTypes, x);
                                   };
            ResetDataContext(connectionString);
        }

        public static IDisposable RestoreDataContext(string connectionString)
        {
            return new MongoQueryableDataService.RestoreDataContextDisposable(connectionString);
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

        protected override DSPQueryableContext CreateDataSource()
        {
            return context;
        }

        protected override DSPMetadata CreateDSPMetadata()
        {
            return metadata;
        }

        internal static new DSPMetadata Metadata
        {
            get { return MongoQueryableDataService.metadata; }
        }
    }
}
