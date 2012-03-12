using System;
using System.Collections.Generic;
using System.Data.Services.Providers;
using System.Linq;
using System.Text;
using DataServiceProvider;
using MongoDB.Bson;

namespace Mongo.Context
{
    internal static class MongoDSPConverter
    {
        public static DSPResource CreateDSPResource(BsonDocument document, DSPMetadata metadata, string namespaceName, string collectionName)
        {
            ResourceType resourceType;
            metadata.TryResolveResourceType(string.Format("{0}.{1}", namespaceName, collectionName), out resourceType);
            var resource = new DSPResource(resourceType);

            foreach (var element in document.Elements)
            {
                if (MongoMetadata.IsObjectId(element))
                {
                    resource.SetValue(MongoMetadata.MappedObjectIdName, element.Value.RawValue.ToString());
                }
                else if (element.Value.GetType() == typeof(BsonDocument))
                {
                    resource.SetValue(element.Name, CreateDSPResource(element.Value.AsBsonDocument, metadata, string.Join(".", namespaceName, collectionName), element.Name));
                }
                else if (element.Value.GetType() == typeof(BsonArray))
                {
                    resource.SetValue(element.Name, element.Value.RawValue);
                    //var bsonArray = element.Value.AsBsonArray;
                    //if (bsonArray != null && bsonArray.Count > 0)
                    //{
                    //    //var tuple = new Tuple<ObjectId, string, List<ObjectId>>(
                    //    //    bsonDocument["_id"].AsObjectId, element.Name, new List<ObjectId>());
                    //    //resourceSetReferences.Add(tuple);
                    //    //foreach (var item in bsonArray)
                    //    //{
                    //    //    tuple.Item3.Add(item.AsBsonDocument["_id"].AsObjectId);
                    //    //}
                    //}
                }
                else if (element.Value.RawValue != null)
                {
                    resource.SetValue(element.Name, element.Value.RawValue);
                }
                else
                {
                    resource.SetValue(element.Name, null);
                }
            }
            return resource;
        }
    }
}
