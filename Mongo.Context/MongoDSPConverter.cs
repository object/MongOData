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
        private static DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DSPResource CreateDSPResource(BsonDocument document, DSPMetadata metadata, string resourceName, string ownerPrefix = null)
        {
            ResourceType resourceType;
            string qualifiedResourceName = string.IsNullOrEmpty(ownerPrefix) ? resourceName : MongoMetadata.GetComplexTypeName(ownerPrefix, resourceName);
            metadata.TryResolveResourceType(string.Join(".", MongoMetadata.RootNamespace, qualifiedResourceName), out resourceType);
            var resource = new DSPResource(resourceType);

            foreach (var element in document.Elements)
            {
                if (MongoMetadata.IsObjectId(element))
                {
                    resource.SetValue(MongoMetadata.MappedObjectIdName, element.Value.RawValue.ToString());
                }
                else if (element.Value.GetType() == typeof(BsonDocument))
                {
                    resource.SetValue(element.Name, CreateDSPResource(element.Value.AsBsonDocument, metadata, element.Name, MongoMetadata.GetComplexTypePrefix(resourceName)));
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
                else
                {
                    object value = null;
                    if (element.Value.RawValue != null)
                    {
                        switch (element.Value.BsonType)
                        {
                            case BsonType.DateTime:
                                value = UnixEpoch + TimeSpan.FromMilliseconds(element.Value.AsBsonDateTime.MillisecondsSinceEpoch);
                                break;
                            default:
                                value = element.Value.RawValue;
                                break;
                        }
                    }
                    else
                    {
                        switch (element.Value.BsonType)
                        {
                            case BsonType.Binary:
                                value = element.Value.AsBsonBinaryData.Bytes;
                                break;
                            default:
                                value = element.Value.RawValue;
                                break;
                        }
                    }
                    resource.SetValue(element.Name, value);
                }
            }
            return resource;
        }
    }
}
