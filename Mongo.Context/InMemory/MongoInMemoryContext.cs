using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataServiceProvider;

namespace Mongo.Context.InMemory
{
    public class MongoInMemoryContext
    {
        public DSPInMemoryContext CreateContext(DSPMetadata metadata, string connectionString)
        {
            var dspContext = new DSPInMemoryContext();
            using (MongoContext mongoContext = new MongoContext(connectionString))
            {
                PopulateData(dspContext, mongoContext, metadata);
            }

            return dspContext;
        }

        private void PopulateData(DSPInMemoryContext dspContext, MongoContext mongoContext, DSPMetadata metadata)
        {
            foreach (var resourceSet in metadata.ResourceSets)
            {
                var storage = dspContext.GetResourceSetStorage(resourceSet.Name);
                var collection = mongoContext.Database.GetCollection(resourceSet.Name);
                foreach (var document in collection.FindAll())
                {
                    var resource = MongoDSPConverter.CreateDSPResource(document, metadata, resourceSet.Name);
                    storage.Add(resource);
                }
            }
        }
    }
}
