using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataServiceProvider;

namespace Mongo.Context.Queryable
{
    public abstract class MongoQueryableDataService : DSPDataService<DSPQueryableContext, MongoDSPResourceQueryProvider>
    {
        protected string connectionString;
        protected static Action<string> ResetDataContext;
        private static DSPQueryableContext context;
        private static new DSPMetadata metadata;

        public MongoQueryableDataService(string connectionString)
        {
            this.connectionString = connectionString;

            ResetDataContext = x =>
                                   {
                                       MongoQueryableDataService.metadata = new MongoMetadata().CreateMetadata(x);
                                       MongoQueryableDataService.context = new MongoQueryableContext().CreateContext(metadata, x);
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
