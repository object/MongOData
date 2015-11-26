using System.Data.Services.Providers;
using DataServiceProvider;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Mongo.Context.InMemory
{
    public abstract class MongoInMemoryDataService : MongoDataServiceBase<DSPInMemoryContext, DSPResourceQueryProvider>
    {
        public MongoInMemoryDataService(string connectionString, MongoConfiguration mongoConfiguration = null)
            : base(connectionString, mongoConfiguration)
        {
        }

        public override DSPInMemoryContext CreateContext(string connectionString)
        {
            var dspContext = new DSPInMemoryContext();
            MongoContext mongoContext = new MongoContext(connectionString);
            PopulateData(dspContext, mongoContext);

            return dspContext;
        }

        private void PopulateData(DSPInMemoryContext dspContext, MongoContext mongoContext)
        {
            foreach (var resourceSet in this.Metadata.ResourceSets)
            {
                var storage = dspContext.GetResourceSetStorage(resourceSet.Name);
                var collection = mongoContext.Database.GetCollection<BsonDocument>(resourceSet.Name);
                var documents = collection.Find(new BsonDocument()).ToListAsync().GetAwaiter().GetResult();
                foreach (var document in documents)
                {
                    var resource = MongoDSPConverter.CreateDSPResource(document, this.mongoMetadata, resourceSet.Name);
                    storage.Add(resource);

                    if (this.mongoMetadata.Configuration.UpdateDynamically)
                    {
                        UpdateMetadataFromResourceSet(mongoContext, resourceSet, document);
                    }
                }
            }
        }

        private void UpdateMetadataFromResourceSet(MongoContext mongoContext, ResourceSet resourceSet, BsonDocument document)
        {
            var resourceType = mongoMetadata.ResolveResourceType(resourceSet.Name);
            foreach (var element in document.Elements)
            {
                mongoMetadata.RegisterResourceProperty(mongoContext, resourceType, element);
            }
        }
    }
}
