using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataServiceProvider;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Mongo.Context
{
    public class MongoDSPUpdateProvider : DSPUpdateProvider
    {
        class ResourceChange
        {
            public string CollectionName { get; set; }
            public DSPResource Resource { get; set; }
            public Dictionary<string, object> ModifiedProperties { get; private set; }

            public ResourceChange(string collectionName, DSPResource resource)
            {
                this.CollectionName = collectionName;
                this.Resource = resource;
                this.ModifiedProperties = new Dictionary<string, object>();
            }
        }

        private string connectionString;
        private MongoMetadata mongoMetadata;
        private List<Tuple<ResourceChange, Action<MongoContext, ResourceChange>>> pendingChanges =
            new List<Tuple<ResourceChange, Action<MongoContext, ResourceChange>>>();

        public MongoDSPUpdateProvider(string connectionString, DSPContext dataContext, MongoMetadata mongoMetadata)
            : base(dataContext, mongoMetadata.CreateDSPMetadata())
        {
            this.connectionString = connectionString;
            this.mongoMetadata = mongoMetadata;
        }

        public override object CreateResource(string containerName, string fullTypeName)
        {
            var resource = base.CreateResource(containerName, fullTypeName) as DSPResource;
            this.pendingChanges.Add(new Tuple<ResourceChange, Action<MongoContext, ResourceChange>>(
                new ResourceChange(containerName, resource), InsertDocument));
            return resource;
        }

        public override void SetValue(object targetResource, string propertyName, object propertyValue)
        {
            base.SetValue(targetResource, propertyName, propertyValue);
            var resource = targetResource as DSPResource;
            var pendingChange = this.pendingChanges.Where(x => x.Item1.Resource == resource && x.Item2 == InsertDocument).SingleOrDefault();
            if (pendingChange == null)
            {
                if (resource.GetValue(propertyName) != propertyValue)
                {
                    pendingChange = this.pendingChanges.Where(x => x.Item1.Resource == resource && x.Item2 == UpdateDocument).SingleOrDefault();
                    if (pendingChange == null)
                    {
                        this.pendingChanges.Add(new Tuple<ResourceChange, Action<MongoContext, ResourceChange>>(
                            new ResourceChange(resource.ResourceType.Name, resource), UpdateDocument));
                    }
                    else
                    {
                        pendingChange.Item1.ModifiedProperties.Add(propertyName, propertyValue);
                    }
                }
            }
        }

        public override void DeleteResource(object targetResource)
        {
            base.DeleteResource(targetResource);
            var resource = targetResource as DSPResource;
            this.pendingChanges.Add(new Tuple<ResourceChange, Action<MongoContext, ResourceChange>>(
                new ResourceChange(resource.ResourceType.Name, resource), RemoveDocument));
        }

        public override void SaveChanges()
        {
            base.SaveChanges();

            using (MongoContext mongoContext = new MongoContext(connectionString))
            {
                foreach (var pendingChange in this.pendingChanges)
                {
                    var action = pendingChange.Item2;
                    action(mongoContext, pendingChange.Item1);
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
            var collection = mongoContext.Database.GetCollection(change.CollectionName);
            var document = MongoDSPConverter.CreateBSonDocument(change.Resource, this.mongoMetadata, change.CollectionName);
            collection.Insert(document);
            change.Resource.SetValue(MongoMetadata.MappedObjectIdName, document.GetValue(MongoMetadata.ProviderObjectIdName).ToString());
        }

        private void UpdateDocument(MongoContext mongoContext, ResourceChange change)
        {
            var collection = mongoContext.Database.GetCollection(change.CollectionName);
            var query = Query.EQ(MongoMetadata.ProviderObjectIdName, ObjectId.Parse(change.Resource.GetValue(MongoMetadata.MappedObjectIdName).ToString()));
            UpdateBuilder update = null;

            foreach (var resourceProperty in change.ModifiedProperties)
            {
                if (update == null)
                {
                    if (resourceProperty.Value != null)
                        update = Update.Set(resourceProperty.Key, BsonValue.Create(resourceProperty.Value));
                    else
                        update = Update.Unset(resourceProperty.Key);
                }
                else
                {
                    if (resourceProperty.Value != null)
                        update = update.Set(resourceProperty.Key, BsonValue.Create(resourceProperty.Value));
                    else
                        update = update.Unset(resourceProperty.Key);
                }
            }

            collection.Update(query, update);
        }

        private void RemoveDocument(MongoContext mongoContext, ResourceChange change)
        {
            var collection = mongoContext.Database.GetCollection(change.CollectionName);
            var query = Query.EQ(MongoMetadata.ProviderObjectIdName, ObjectId.Parse(change.Resource.GetValue(MongoMetadata.MappedObjectIdName).ToString()));
            collection.Remove(query);
        }
    }
}
