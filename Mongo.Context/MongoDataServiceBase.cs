

using System;
using DataServiceProvider;

namespace Mongo.Context
{
    public abstract class MongoDataServiceBase<T, Q> : DSPDataService<T, Q, DSPUpdateProvider>
        where T : DSPContext
        where Q : DSPResourceQueryProvider
    {
        protected string connectionString;
        protected MongoConfiguration mongoConfiguration;
        protected static Action<string> ResetDataContext;
        protected static T context;
        protected MongoMetadata mongoMetadata;

        /// <summary>Constructor</summary>
        public MongoDataServiceBase(string connectionString, MongoConfiguration mongoConfiguration)
        {
            this.connectionString = connectionString;
            this.mongoConfiguration = mongoConfiguration;
            this.createUpdateProvider = () => new MongoDSPUpdateProvider(this.connectionString, this.CurrentDataSource, this.mongoMetadata);

            ResetDataContext = x =>
            {
                this.mongoMetadata = new MongoMetadata(x, this.mongoConfiguration == null ? null : this.mongoConfiguration.MetadataBuildStrategy);
                MongoDataServiceBase<T, Q>.context = this.CreateContext(x);
            };

            ResetDataContext(connectionString);
        }

        public static IDisposable RestoreDataContext(string connectionString)
        {
            return new MongoDataServiceBase<T, Q>.RestoreDataContextDisposable(connectionString);
        }

        public abstract T CreateContext(string connectionString);

        private class RestoreDataContextDisposable : IDisposable
        {
            private readonly string _connectionString;

            public RestoreDataContextDisposable(string connectionString)
            {
                _connectionString = connectionString;
            }

            public void Dispose()
            {
                ResetDataContext(_connectionString);
            }
        }

        protected override T CreateDataSource()
        {
            return context;
        }

        protected override DSPMetadata CreateDSPMetadata()
        {
            lock (this)
            {
                if (this.metadata == null)
                {
                    this.metadata = this.mongoMetadata.CreateDSPMetadata();
                }
            }
            return metadata;
        }

        public static void ResetDSPMetadata()
        {
            MongoMetadata.ResetDSPMetadata();
        }
    }
}
