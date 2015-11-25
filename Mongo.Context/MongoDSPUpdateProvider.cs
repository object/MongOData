using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using DataServiceProvider;

namespace Mongo.Context
{
    public class MongoDSPUpdateProvider : DSPUpdateProvider
    {
        class ResourceChange
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

        private string connectionString;
        private MongoMetadata mongoMetadata;
        private List<ResourceChange> pendingChanges = new List<ResourceChange>();

        public MongoDSPUpdateProvider(string connectionString, DSPContext dataContext, MongoMetadata mongoMetadata)
            : base(dataContext, mongoMetadata.CreateDSPMetadata())
        {
            this.connectionString = connectionString;
            this.mongoMetadata = mongoMetadata;
        }

        public override object CreateResource(string containerName, string fullTypeName)
        {
            var resource = base.CreateResource(containerName, fullTypeName) as DSPResource;

            this.pendingChanges.Add(new ResourceChange(containerName, resource, InsertDocument));
            return resource;
        }

        public override void SetValue(object targetResource, string propertyName, object propertyValue)
        {
            base.SetValue(targetResource, propertyName, propertyValue);

            var resource = targetResource as DSPResource;
            var pendingChange = this.pendingChanges.SingleOrDefault(x => x.Resource == resource && x.Action == InsertDocument);
            if (pendingChange == null)
            {
                pendingChange = this.pendingChanges.SingleOrDefault(x => x.Resource == resource && x.Action == UpdateDocument);
                if (pendingChange == null)
                {
                    pendingChange = new ResourceChange(resource.ResourceType.Name, resource, UpdateDocument);
                    this.pendingChanges.Add(pendingChange);
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
            this.pendingChanges.Add(new ResourceChange(resource.ResourceType.Name, resource, RemoveDocument));
        }

        public override void SaveChanges()
        {
            base.SaveChanges();

            using (MongoContext mongoContext = new MongoContext(connectionString))
            {
                foreach (var pendingChange in this.pendingChanges)
                {
                    var action = pendingChange.Action;
                    action(mongoContext, pendingChange);
                }
            }

            this.pendingChanges.Clear();
        }

        public override void ClearChanges()
        {
            base.ClearChanges();

            this.pendingChanges.Clear();
        }

        private void InsertDocument(MongoContext mongoContext, ResourceChange change)
        {
            //var collection = mongoContext.Database.GetCollection(change.CollectionName);
            //var document = MongoDSPConverter.CreateBSonDocument(change.Resource, this.mongoMetadata, change.CollectionName);
            //collection.Insert(document);
            //change.Resource.SetValue(MongoMetadata.MappedObjectIdName, document.GetValue(MongoMetadata.ProviderObjectIdName).ToString());
        }

        private void UpdateDocument(MongoContext mongoContext, ResourceChange change)
        {
            //if (!change.ModifiedProperties.Any())
            //    return;

            //var collection = mongoContext.Database.GetCollection(change.CollectionName);
            //var query = Query.EQ(MongoMetadata.ProviderObjectIdName, ObjectId.Parse(change.Resource.GetValue(MongoMetadata.MappedObjectIdName).ToString()));
            //UpdateBuilder update = null;

            //foreach (var resourceProperty in change.ModifiedProperties)
            //{
            //    if (update == null)
            //    {
            //        if (resourceProperty.Value != null)
            //            update = Update.Set(resourceProperty.Key, BsonValue.Create(resourceProperty.Value));
            //        else
            //            update = Update.Unset(resourceProperty.Key);
            //    }
            //    else
            //    {
            //        if (resourceProperty.Value != null)
            //            update = update.Set(resourceProperty.Key, BsonValue.Create(resourceProperty.Value));
            //        else
            //            update = update.Unset(resourceProperty.Key);
            //    }
            //}

            //collection.Update(query, update);
        }

        private void RemoveDocument(MongoContext mongoContext, ResourceChange change)
        {
            //var collection = mongoContext.Database.GetCollection(change.CollectionName);
            //var query = Query.EQ(MongoMetadata.ProviderObjectIdName, ObjectId.Parse(change.Resource.GetValue(MongoMetadata.MappedObjectIdName).ToString()));
            //collection.Remove(query);
        }
    }
}
