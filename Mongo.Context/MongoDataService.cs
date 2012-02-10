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

namespace Mongo.Context
{
    public abstract class MongoDataService<TDSPContext, TDSPMetadata> : DSPDataService<DSPContext>
        where TDSPContext : IMongoDSPContext, new()
        where TDSPMetadata : IMongoDSPMetadata, new()
    {
        private string connectionString;
        private static DSPContext context;
        private static DSPMetadata metadata;

        /// <summary>Constructor</summary>
        public MongoDataService(string connectionString)
        {
            this.connectionString = connectionString;
            context = CreateDataContext(connectionString);
        }

        public static void ResetDataContext(string connectionString)
        {
            context = CreateDataContext(connectionString);
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

        protected override DSPContext CreateDataSource()
        {
            return context;
        }

        protected override DSPMetadata CreateDSPMetadata()
        {
            return metadata;
        }

        internal static DSPContext CreateDataContext(string connectionString)
        {
            metadata = new TDSPMetadata().CreateMetadata(connectionString);
            return new TDSPContext().CreateContext(metadata, connectionString);
        }
    }
}
