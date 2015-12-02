using System;
using System.Collections.Generic;
using System.Linq;
using DataServiceProvider;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Mongo.Context
{
    public class MongoDSPUpdateProvider : DSPUpdateProvider
    {
        private class ResourceChange
        {
            public string CollectionName { get; set; }
            public DSPResource Resource { get; set; }
            public Dictionary<string, object> ModifiedProperties { get; private set; }
            public Action<MongoContext, ResourceChange> Action { get; private set; }

            public ResourceChange(string collectionName, DSPResource resource, Action<MongoContext, ResourceChange> action)
            {
                this.CollectionName = collectionName;
                this.Resource = resource;
                this.Action = action;
                this.ModifiedProperties = new Dictionary<string, object>();
            }
        }

        private string _connectionString;
        private MongoMetadata _mongoMetadata;
        private List<ResourceChange> _pendingChanges = new List<ResourceChange>();

        public MongoDSPUpdateProvider(string connectionString, DSPContext dataContext, MongoMetadata mongoMetadata)
            : base(dataContext, mongoMetadata.CreateDSPMetadata())
        {
            _connectionString = connectionString;
            _mongoMetadata = mongoMetadata;
        }

        public override object CreateResource(string containerName, string fullTypeName)
        {
            var resource = base.CreateResource(containerName, fullTypeName) as DSPResource;

            _pendingChanges.Add(new ResourceChange(containerName, resource, InsertDocument));
            return resource;
        }

        public override void SetValue(object targetResource, string propertyName, object propertyValue)
        {
            base.SetValue(targetResource, propertyName, propertyValue);

            var resource = targetResource as DSPResource;
            var pendingChange = _pendingChanges.SingleOrDefault(x => x.Resource == resource && x.Action == InsertDocument);
            if (pendingChange == null)
            {
                pendingChange = _pendingChanges.SingleOrDefault(x => x.Resource == resource && x.Action == UpdateDocument);
                if (pendingChange == null)
                {
                    pendingChange = new ResourceChange(resource.ResourceType.Name, resource, UpdateDocument);
                    _pendingChanges.Add(pendingChange);
                }
            }

            var properties = pendingChange.ModifiedProperties;
            if (properties.ContainsKey(propertyName))
                properties[propertyName] = propertyValue;
            else
                properties.Add(propertyName, propertyValue);
        }

        public override void DeleteResource(object targetResource)
        {
            base.DeleteResource(targetResource);

            var resource = targetResource as DSPResource;
            _pendingChanges.Add(new ResourceChange(resource.ResourceType.Name, resource, RemoveDocument));
        }

        public override void SaveChanges()
        {
            base.SaveChanges();

            MongoContext mongoContext = new MongoContext(_connectionString);

            foreach (var pendingChange in _pendingChanges)
            {
                var action = pendingChange.Action;
                action(mongoContext, pendingChange);
            }

            _pendingChanges.Clear();
        }

        public override void ClearChanges()
        {
            base.ClearChanges();

            _pendingChanges.Clear();
        }

        private void InsertDocument(MongoContext mongoContext, ResourceChange change)
        {
            var collection = mongoContext.Database.GetCollection<BsonDocument>(change.CollectionName);
            var document = MongoDSPConverter.CreateBSonDocument(change.Resource, _mongoMetadata, change.CollectionName);
            collection.InsertOneAsync(document).GetAwaiter().GetResult();
            change.Resource.SetValue(MongoMetadata.MappedObjectIdName, document.GetValue(MongoMetadata.ProviderObjectIdName).ToString());
        }

        private void UpdateDocument(MongoContext mongoContext, ResourceChange change)
        {
            if (!change.ModifiedProperties.Any())
                return;

            var collection = mongoContext.Database.GetCollection<BsonDocument>(change.CollectionName);
            var filter = Builders<BsonDocument>.Filter.Eq(MongoMetadata.ProviderObjectIdName, ObjectId.Parse(change.Resource.GetValue(MongoMetadata.MappedObjectIdName).ToString()));

            UpdateDefinition<BsonDocument> update = null;

            foreach (var resourceProperty in change.ModifiedProperties)
            {
                if (update == null)
                {
                    if (resourceProperty.Value != null)
                    {
                        update = Builders<BsonDocument>.Update.Set(resourceProperty.Key, BsonValue.Create(resourceProperty.Value));
                    }
                    else
                    {
                        update = Builders<BsonDocument>.Update.Unset(resourceProperty.Key);
                    }
                }
                else
                {
                    if (resourceProperty.Value != null)
                    {
                        update = update.Set(resourceProperty.Key, BsonValue.Create(resourceProperty.Value));
                    }
                    else
                    {
                        update = update.Unset(resourceProperty.Key);
                    }
                }
            }

            collection.UpdateOneAsync(filter, update).GetAwaiter().GetResult();
        }

        private void RemoveDocument(MongoContext mongoContext, ResourceChange change)
        {
            var collection = mongoContext.Database.GetCollection<BsonDocument>(change.CollectionName);
            var filter = Builders<BsonDocument>.Filter.Eq(MongoMetadata.ProviderObjectIdName, ObjectId.Parse(change.Resource.GetValue(MongoMetadata.MappedObjectIdName).ToString()));
            collection.DeleteOneAsync(filter).GetAwaiter().GetResult();
        }
    }
}
