using System;
using System.Collections.Generic;
using System.Data.Services.Providers;
using System.Linq;
using System.Text;
using DataServiceProvider;

namespace Mongo.Context.InMemory
{
    public abstract class MongoInMemoryDataService : MongoDataServiceBase<DSPInMemoryContext, DSPResourceQueryProvider>
    {
        /// <summary>Constructor</summary>
        public MongoInMemoryDataService(string connectionString, MongoConfiguration mongoConfiguration = null)
            : base(connectionString, mongoConfiguration)
        {
        }

        public override DSPInMemoryContext CreateContext(string connectionString)
        {
            var dspContext = new DSPInMemoryContext();
            using (MongoContext mongoContext = new MongoContext(connectionString))
            {
                PopulateData(dspContext, mongoContext);
            }

            return dspContext;
        }

        private void PopulateData(DSPInMemoryContext dspContext, MongoContext mongoContext)
        {
            foreach (var resourceSet in this.Metadata.ResourceSets)
            {
                var storage = dspContext.GetResourceSetStorage(resourceSet.Name);
                var collection = mongoContext.Database.GetCollection(resourceSet.Name);
                foreach (var document in collection.FindAll())
                {
                    var resource = MongoDSPConverter.CreateDSPResource(document, this.mongoMetadata, resourceSet.Name);
                    storage.Add(resource);

                    if (this.mongoMetadata.Configuration.UpdateDynamically)
                    {
                        var resourceType = mongoMetadata.ResolveResourceType(resourceSet.Name);
                        foreach (var element in document.Elements)
                        {
                            var propertyName = MongoMetadata.GetResourcePropertyName(element);
                            var resourceProperty = resourceType.Properties.SingleOrDefault(x => x.Name == propertyName);
                            if (resourceProperty == null)
                            {
                                mongoMetadata.UpdateResourceType(mongoContext, resourceType, element);
                            }
                        }
                    }
                }
            }
        }
    }
}
