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
            if (resourceType == null)
                throw new ArgumentException(string.Format("Unable to resolve resource type {0}", resourceName), "resourceName");
            var resource = new DSPResource(resourceType);

            foreach (var element in document.Elements)
            {
                var resourceProperty = mongoMetadata.ResolveResourceProperty(resourceType, element);
                if (resourceProperty == null)
                    continue;

                string propertyName = MongoMetadata.GetResourcePropertyName(element);
                object propertyValue = null;

                if (MongoMetadata.IsObjectId(element))
                {
                    propertyValue = element.Value.RawValue.ToString();
                }
                else if (element.Value.GetType() == typeof(BsonDocument))
                {
                    propertyValue = CreateDSPResource(element.Value.AsBsonDocument, mongoMetadata, propertyName,
                        MongoMetadata.GetComplexTypePrefix(resourceType.Name));
                }
                else if (element.Value.GetType() == typeof(BsonArray))
                {
                    var bsonArray = element.Value.AsBsonArray;
                    if (bsonArray != null && bsonArray.Count > 0)
                    {
                        int nonNullItemCount = 0;
                        for (int index = 0; index < bsonArray.Count; index++)
                        {
                            if (bsonArray[index] != BsonNull.Value)
                                ++nonNullItemCount;
                        }
                        var valueArray = new DSPResource[nonNullItemCount];
                        int valueIndex = 0;
                        for (int index = 0; index < bsonArray.Count; index++)
                        {
                            if (bsonArray[index] != BsonNull.Value)
                            {
                                valueArray[valueIndex++] = CreateDSPResource(bsonArray[index].AsBsonDocument, mongoMetadata, propertyName,
                                    MongoMetadata.GetCollectionTypePrefix(resourceType.Name));
                            }
                        }
                        propertyValue = valueArray;
                    }
                }
                else
                {
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

                if (propertyValue != null && element.Value.GetType() != typeof(BsonArray))
                {
                    var propertyType = resourceProperty.ResourceType.InstanceType;
                    Type underlyingNonNullableType = Nullable.GetUnderlyingType(resourceProperty.ResourceType.InstanceType);
                    if (underlyingNonNullableType != null)
                    {
                        propertyType = underlyingNonNullableType;
                    }
                    propertyValue = Convert.ChangeType(propertyValue, propertyType);
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
