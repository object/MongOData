using System;
using System.Collections.Generic;
using System.Data.Services.Providers;
using System.Linq;
using System.Text;
using DataServiceProvider;
using MongoDB.Bson;

namespace Mongo.Context
{
    public class MongoMetadata
    {
        public static readonly string ProviderObjectIdName = "_id";
        public static readonly string MappedObjectIdName = "db_id";
        public static readonly Type MappedObjectIdType = typeof(string);
        public static readonly string ContainerName = "MongoContext";
        public static readonly string RootNamespace = "Mongo";
        public static readonly bool UseGlobalComplexTypeNames = true;

        public DSPMetadata Metadata { get; private set; }
        private Dictionary<string, Type> providerTypes = new Dictionary<string, Type>();
        public Dictionary<string, Type> ProviderTypes { get { return this.providerTypes; } }

        public MongoMetadata(string connectionString)
        {
            this.Metadata = new DSPMetadata(ContainerName, RootNamespace);

            using (MongoContext context = new MongoContext(connectionString))
            {
                PopulateMetadata(context);
            }
        }

        protected IEnumerable<string> GetCollectionNames(MongoContext mongoContext)
        {
            return mongoContext.Database.GetCollectionNames().Where(x => !x.StartsWith("system."));
        }

        protected void PopulateMetadata(MongoContext context)
        {
            foreach (var collectionName in GetCollectionNames(context))
            {
                var collection = context.Database.GetCollection(collectionName);
                var document = collection.FindOne();
                if (document != null)
                {
                    var resourceSet = this.Metadata.ResourceSets.SingleOrDefault(x => x.Name == collectionName);
                    if (resourceSet == null)
                    {
                        RegisterResourceType(this.Metadata, context, collectionName, document, ResourceTypeKind.EntityType);
                    }
                }
            }
        }

        private ResourceType RegisterResourceType(DSPMetadata metadata, MongoContext context,
            string collectionName, BsonDocument document, ResourceTypeKind resourceTypeKind)
        {
            var collectionType = resourceTypeKind == ResourceTypeKind.EntityType
                                     ? metadata.AddEntityType(collectionName)
                                     : metadata.AddComplexType(collectionName);

            bool hasObjectId = false;
            foreach (var element in document.Elements)
            {
                var elementType = GetElementType(element);
                RegisterResourceProperty(metadata, context, collectionName, collectionType, elementType, element, resourceTypeKind);
                if (IsObjectId(element))
                    hasObjectId = true;
            }

            if (resourceTypeKind == ResourceTypeKind.EntityType)
            {
                if (!hasObjectId)
                    metadata.AddKeyProperty(collectionType, MappedObjectIdName, MappedObjectIdType);

                metadata.AddResourceSet(collectionName, collectionType);
            }

            return collectionType;
        }

        private void RegisterResourceProperty(DSPMetadata metadata, MongoContext context, 
            string collectionName, ResourceType collectionType, Type elementType, BsonElement element, ResourceTypeKind resourceTypeKind)
        {
            if (IsObjectId(element))
            {
                if (resourceTypeKind == ResourceTypeKind.EntityType)
                    metadata.AddKeyProperty(collectionType, MappedObjectIdName, elementType);
                else
                    metadata.AddPrimitiveProperty(collectionType, MappedObjectIdName, elementType);
                AddProviderType(collectionName, MappedObjectIdName, element.Value);
            }
            else if (elementType == typeof (BsonDocument))
            {
                ResourceType resourceType = null;
                var resourceSet = metadata.ResourceSets.SingleOrDefault(x => x.Name == element.Name);
                if (resourceSet != null)
                {
                    resourceType = resourceSet.ResourceType;
                }
                else
                {
                    resourceType = RegisterResourceType(metadata, context,
                                                        GetComplexTypeName(collectionName, element.Name),
                                                        element.Value.AsBsonDocument, ResourceTypeKind.ComplexType);
                }
                metadata.AddComplexProperty(collectionType, element.Name, resourceType);
                AddProviderType(collectionName, element.Name, element.Value);
            }
            else if (elementType == typeof (BsonArray))
            {
                var bsonArray = element.Value.AsBsonArray;
                if (bsonArray != null && bsonArray.Count > 0)
                {
                    // TODO
                    //var referencedCollection = GetDocumentCollection(context, bsonArray[0].AsBsonDocument);
                    //if (referencedCollection != null)
                    //{
                    //    resourceReferences.Add(new Tuple<ResourceType, string, string>(collectionType, element.Name, referencedCollection.Name));
                    //}
                }
            }
            else
            {
                metadata.AddPrimitiveProperty(collectionType, element.Name, elementType);
                AddProviderType(collectionName, element.Name, element.Value);
            }
        }

        private void AddProviderType(string collectionName, string elementName, BsonValue elementValue)
        {
            string typeName = string.Join(".", collectionName, elementName);
            if (elementValue.BsonType == BsonType.Document)
            {
                this.providerTypes.Add(typeName, typeof(BsonDocument));
            }
            else if (elementValue.RawValue != null)
            {
                switch (elementValue.BsonType)
                {
                    case BsonType.DateTime:
                        this.providerTypes.Add(typeName, typeof(DateTime));
                        break;
                    default:
                        this.providerTypes.Add(typeName, elementValue.RawValue.GetType());
                        break;
                }
            }
        }

        public static bool IsObjectId(BsonElement element)
        {
            return element.Value.RawValue != null &&
                (element.Value.RawValue.GetType() == typeof(ObjectId) || element.Value.RawValue.GetType() == typeof(BsonObjectId));
        }

        private static Type GetElementType(BsonElement element)
        {
            if (IsObjectId(element))
            {
                return MappedObjectIdType;
            }
            else if (element.Value.RawValue != null)
            {
                switch (element.Value.BsonType)
                {
                    case BsonType.DateTime:
                        return typeof(DateTime);
                    default:
                        return element.Value.RawValue.GetType();
                }
            }
            else if (element.Value.GetType() == typeof(BsonArray) || element.Value.GetType() == typeof(BsonDocument))
            {
                return element.Value.GetType();
            }
            else
            {
                switch (element.Value.BsonType)
                {
                    case BsonType.Binary:
                        return typeof(byte[]);
                    default:
                        return typeof(string);
                }
            }
        }

        internal static string GetComplexTypePrefix(string ownerName)
        {
            return UseGlobalComplexTypeNames ? string.Empty : ownerName;
        }

        internal static string GetComplexTypeName(string collectionName, string resourceName)
        {
            return UseGlobalComplexTypeNames ? resourceName : string.Join("__", collectionName, resourceName);
        }
    }
}
