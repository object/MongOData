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

        public static DSPResource CreateDSPResource(BsonDocument document, MongoMetadata mongoMetadata, string resourceName, string ownerPrefix = null)
        {
            var resourceType = mongoMetadata.ResolveResourceType(resourceName, ownerPrefix);
            var resource = new DSPResource(resourceType);

            foreach (var element in document.Elements)
            {
                var resourceProperty = mongoMetadata.ResolveResourceProperty(resourceType, element);
                if (resourceProperty == null)
                    continue;

                string propertyName = null;
                object propertyValue = null;

                if (MongoMetadata.IsObjectId(element))
                {
                    propertyName = MongoMetadata.MappedObjectIdName;
                    propertyValue = element.Value.RawValue.ToString();
                }
                else if (element.Value.GetType() == typeof(BsonDocument))
                {
                    propertyName = element.Name;
                    propertyValue = CreateDSPResource(element.Value.AsBsonDocument, mongoMetadata, element.Name,
                                                      MongoMetadata.GetComplexTypePrefix(resourceName));
                }
                else if (element.Value.GetType() == typeof(BsonArray))
                {
                    propertyName = element.Name;
                    propertyValue = element.Value.RawValue;

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
                    propertyName = element.Name;
                    if (element.Value.RawValue != null)
                    {
                        switch (element.Value.BsonType)
                        {
                            case BsonType.DateTime:
                                propertyValue = UnixEpoch + TimeSpan.FromMilliseconds(element.Value.AsBsonDateTime.MillisecondsSinceEpoch);
                                break;
                            default:
                                propertyValue = element.Value.RawValue;
                                break;
                        }
                    }
                    else
                    {
                        switch (element.Value.BsonType)
                        {
                            case BsonType.Binary:
                                propertyValue = element.Value.AsBsonBinaryData.Bytes;
                                break;
                            default:
                                propertyValue = element.Value.RawValue;
                                break;
                        }
                    }
                }

                if (propertyValue != null)
                {
                    propertyValue = Convert.ChangeType(propertyValue, resourceProperty.ResourceType.InstanceType);
                }
                resource.SetValue(propertyName, propertyValue);
            }

            return resource;
        }

        public static BsonDocument CreateBSonDocument(DSPResource resource, MongoMetadata mongoMetadata, string resourceName)
        {
            var document = new BsonDocument();
            var resourceSet = mongoMetadata.ResolveResourceSet(resourceName);
            if (resourceSet != null)
            {
                foreach (var property in resourceSet.ResourceType.Properties)
                {
                    var propertyValue = resource.GetValue(property.Name);
                    if (propertyValue != null)
                    {
                        document.Set(property.Name, BsonValue.Create(propertyValue));
                    }
                }
            }
            return document;
        }
    }
}
