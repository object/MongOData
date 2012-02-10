using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using DataServiceProvider;

namespace Mongo.Context
{
    public abstract class MongoMetadataBase : IMongoDSPMetadata
    {
        public DSPMetadata CreateMetadata(string connectionString)
        {
            var metadata = new DSPMetadata("MongoContext", "Mongo");

            using (MongoContext context = new MongoContext(connectionString))
            {
                PopulateMetadata(metadata, context);
            }

            return metadata;
        }

        protected abstract void PopulateMetadata(DSPMetadata metadata, MongoContext context);

        protected IEnumerable<string> GetCollectionNames(MongoContext mongoContext)
        {
            return mongoContext.Database.GetCollectionNames().Where(x => !x.StartsWith("system."));
        }
    }
}
