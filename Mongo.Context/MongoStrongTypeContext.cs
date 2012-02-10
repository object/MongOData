using System;
using System.Collections.Generic;
using System.Data.Services.Providers;
using System.Linq;
using System.Text;
using DataServiceProvider;
using MongoDB.Bson;

namespace Mongo.Context
{
    public class MongoStrongTypeContext : IMongoDSPContext
    {
        private static DSPContext cachedContext = null;
        private Dictionary<ObjectId, DSPResource> resourceMap = new Dictionary<ObjectId, DSPResource>();
        private List<Tuple<ObjectId, string, ObjectId>> resourceReferences = new List<Tuple<ObjectId, string, ObjectId>>();
        private List<Tuple<ObjectId, string, List<ObjectId>>> resourceSetReferences = new List<Tuple<ObjectId, string, List<ObjectId>>>();

        public DSPContext CreateContext(DSPMetadata metadata, string connectionString)
        {
            if (cachedContext == null)
            {
                lock (this)
                {
                    var dspContext = new DSPContext();
                    using (MongoContext mongoContext = new MongoContext(connectionString))
                    {
                        foreach (var resourceSet in metadata.ResourceSets)
                        {
                            var entities = dspContext.GetResourceSetEntities(resourceSet.Name);
                            foreach (var bsonDocument in mongoContext.Database.GetCollection(resourceSet.Name).FindAll())
                            {
                                var resource = CreateDSPResource(resourceSet.ResourceType, bsonDocument);
                                entities.Add(resource);
                            }
                        }
                    }

                    foreach (var reference in resourceReferences)
                    {
                        resourceMap[reference.Item1].SetValue(reference.Item2, resourceMap[reference.Item3]);
                    }

                    foreach (var reference in resourceSetReferences)
                    {
                        var referencedCollection = new List<DSPResource>();
                        resourceMap[reference.Item1].SetValue(reference.Item2, referencedCollection);
                        foreach (var objectId in reference.Item3)
                        {
                            referencedCollection.Add(resourceMap[objectId]);
                        }
                    }

                    cachedContext = dspContext;
                }
            }
            return cachedContext;
        }

        private DSPResource CreateDSPResource(ResourceType resourceType, BsonDocument bsonDocument)
        {
            var resource = new DSPResource(resourceType);
            foreach (var element in bsonDocument.Elements)
            {
                if (element.Name == "_id")
                {
                    resource.SetValue("ID", element.Value.ToString());
                    this.resourceMap.Add(element.Value.AsObjectId, resource);
                }
                else if (element.Value.GetType() == typeof(BsonDocument))
                {
                    this.resourceReferences.Add(new Tuple<ObjectId, string, ObjectId>(
                        bsonDocument["_id"].AsObjectId, element.Name, element.Value.AsBsonDocument["_id"].AsObjectId));
                }
                else if (element.Value.GetType() == typeof(BsonArray))
                {
                    var bsonArray = element.Value.AsBsonArray;
                    if (bsonArray != null && bsonArray.Count > 0)
                    {
                        var tuple = new Tuple<ObjectId, string, List<ObjectId>>(
                            bsonDocument["_id"].AsObjectId, element.Name, new List<ObjectId>());
                        resourceSetReferences.Add(tuple);
                        foreach (var item in bsonArray)
                        {
                            tuple.Item3.Add(item.AsBsonDocument["_id"].AsObjectId);
                        }
                    }
                }
                else if (element.Value.RawValue != null)
                {
                    resource.SetValue(element.Name, element.Value.RawValue);
                }
            }
            return resource;
        }
    }
}
