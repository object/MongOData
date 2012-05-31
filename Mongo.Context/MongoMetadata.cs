using System;
using System.Collections.Generic;
using System.Data.Services.Providers;
using System.Linq;
using System.Text;
using DataServiceProvider;
using MongoDB.Bson;

namespace Mongo.Context
{
    class MongoMetadataCache
    {
        public DSPMetadata DspMetadata { get; set; }
        public Dictionary<string, Type> ProviderTypes { get; set; }
    }

    public class MongoMetadata
    {
        public static readonly string ProviderObjectIdName = "_id";
        public static readonly string MappedObjectIdName = "db_id";
        public static readonly Type MappedObjectIdType = typeof(string);
        public static readonly string ContainerName = "MongoContext";
        public static readonly string RootNamespace = "Mongo";
        public static readonly bool UseGlobalComplexTypeNames = true;

        private string connectionString;
        private DSPMetadata dspMetadata;
        private readonly Dictionary<string, Type> providerTypes;
        private static readonly Dictionary<string, MongoMetadataCache> MetadataCache = new Dictionary<string, MongoMetadataCache>();

        public MongoConfiguration.Metadata Configuration { get; private set; }
        public Dictionary<string, Type> ProviderTypes { get { return this.providerTypes; } }

        public MongoMetadata(string connectionString, MongoConfiguration.Metadata metadata = null)
        {
            this.connectionString = connectionString;
            this.Configuration = metadata ?? MongoConfiguration.Metadata.Default;
            lock (MetadataCache)
            {
                MongoMetadataCache metadataCache;
                MetadataCache.TryGetValue(this.connectionString, out metadataCache);
                if (metadataCache == null)
                {
                    metadataCache = new MongoMetadataCache
                                        {
                                            DspMetadata = new DSPMetadata(ContainerName, RootNamespace),
                                            ProviderTypes = new Dictionary<string, Type>()
                                        };
                    MetadataCache.Add(this.connectionString, metadataCache);
                }
                this.dspMetadata = metadataCache.DspMetadata;
                this.providerTypes = metadataCache.ProviderTypes;
            }

            using (var context = new MongoContext(connectionString))
            {
                PopulateMetadata(context);
            }
        }

        public DSPMetadata CreateDSPMetadata()
        {
            return this.dspMetadata.Clone() as DSPMetadata;
        }

        public static void ResetDSPMetadata()
        {
            MetadataCache.Clear();
        }

        public ResourceType ResolveResourceType(string resourceName, string ownerPrefix = null)
        {
            ResourceType resourceType;
            string qualifiedResourceName = string.IsNullOrEmpty(ownerPrefix) ? resourceName : MongoMetadata.GetComplexTypeName(ownerPrefix, resourceName);
            this.dspMetadata.TryResolveResourceType(string.Join(".", MongoMetadata.RootNamespace, qualifiedResourceName), out resourceType);
            return resourceType;
        }

        public ResourceSet ResolveResourceSet(string resourceName)
        {
            return this.dspMetadata.ResourceSets.SingleOrDefault(x => x.Name == resourceName);
        }

        public ResourceProperty ResolveResourceProperty(ResourceType resourceType, BsonElement element)
        {
            var propertyName = MongoMetadata.GetResourcePropertyName(element);
            return resourceType.Properties.SingleOrDefault(x => x.Name == propertyName);
        }

        private IEnumerable<string> GetCollectionNames(MongoContext context)
        {
            return context.Database.GetCollectionNames().Where(x => !x.StartsWith("system."));
        }

        private void PopulateMetadata(MongoContext context)
        {
            foreach (var collectionName in GetCollectionNames(context))
            {
                var collection = context.Database.GetCollection(collectionName);
                var resourceSet = this.dspMetadata.ResourceSets.SingleOrDefault(x => x.Name == collectionName);

                var documents = collection.FindAll();
                if (this.Configuration.PrefetchRows == 0)
                {
                    if (resourceSet == null)
                    {
                        RegisterResourceSet(context, collectionName);
                    }
                }
                else
                {
                    int rowCount = 0;
                    foreach (var document in documents)
                    {
                        if (resourceSet == null)
                        {
                            resourceSet = RegisterResourceSet(context, collectionName, document);
                        }
                        else
                        {
                            UpdateResourceSet(context, resourceSet, document);
                        }

                        ++rowCount;
                        if (this.Configuration.PrefetchRows >= 0 && rowCount >= this.Configuration.PrefetchRows)
                            break;
                    }
                }
            }
        }

        private ResourceSet RegisterResourceSet(MongoContext context, string collectionName, BsonDocument document = null)
        {
            RegisterResourceType(context, collectionName, document, ResourceTypeKind.EntityType);
            return this.dspMetadata.ResourceSets.SingleOrDefault(x => x.Name == collectionName);
        }

        private void UpdateResourceSet(MongoContext context, ResourceSet resourceSet, BsonDocument document)
        {
            foreach (var element in document.Elements)
            {
                var propertyName = GetResourcePropertyName(element);
                var resourceProperty = resourceSet.ResourceType.Properties.Where(x => x.Name == propertyName).SingleOrDefault();
                if (resourceProperty == null)
                {
                    var elementType = GetElementType(element);
                    RegisterResourceProperty(context, resourceSet.Name, resourceSet.ResourceType, elementType, element, ResourceTypeKind.EntityType);
                }
            }
        }

        private ResourceType RegisterResourceType(MongoContext context, string collectionName,
            BsonDocument document, ResourceTypeKind resourceTypeKind)
        {
            var collectionType = resourceTypeKind == ResourceTypeKind.EntityType
                                     ? this.dspMetadata.AddEntityType(collectionName)
                                     : this.dspMetadata.AddComplexType(collectionName);

            bool hasObjectId = false;
            if (document != null)
            {
                foreach (var element in document.Elements)
                {
                    var elementType = GetElementType(element);
                    RegisterResourceProperty(context, collectionName, collectionType, elementType, element, resourceTypeKind);
                    if (IsObjectId(element))
                        hasObjectId = true;
                }
            }

            if (resourceTypeKind == ResourceTypeKind.EntityType)
            {
                if (!hasObjectId)
                {
                    this.dspMetadata.AddKeyProperty(collectionType, MappedObjectIdName, MappedObjectIdType);
                    AddProviderType(collectionName, MappedObjectIdName, BsonObjectId.Empty, true);
                }

                this.dspMetadata.AddResourceSet(collectionName, collectionType);
            }

            return collectionType;
        }

        private void RegisterResourceProperty(MongoContext context, string collectionName, ResourceType collectionType,
            Type elementType, BsonElement element, ResourceTypeKind resourceTypeKind)
        {
            if (ResolveProviderType(element.Value) == null)
                return;

            string propertyName = null;
            var propertyValue = element.Value;
            var isKey = false;
            if (IsObjectId(element))
            {
                propertyName = MongoMetadata.MappedObjectIdName;
                if (resourceTypeKind == ResourceTypeKind.EntityType)
                    this.dspMetadata.AddKeyProperty(collectionType, propertyName, elementType);
                else
                    this.dspMetadata.AddPrimitiveProperty(collectionType, propertyName, elementType);
                isKey = true;
            }
            else if (elementType == typeof(BsonDocument))
            {
                propertyName = element.Name;
                ResourceType resourceType = null;
                var resourceSet = this.dspMetadata.ResourceSets.SingleOrDefault(x => x.Name == element.Name);
                if (resourceSet != null)
                {
                    resourceType = resourceSet.ResourceType;
                }
                else
                {
                    resourceType = RegisterResourceType(context, GetComplexTypeName(collectionName, element.Name),
                                                        element.Value.AsBsonDocument, ResourceTypeKind.ComplexType);
                }
                this.dspMetadata.AddComplexProperty(collectionType, element.Name, resourceType);
            }
            else if (elementType == typeof(BsonArray))
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
                propertyName = element.Name;
                this.dspMetadata.AddPrimitiveProperty(collectionType, element.Name, elementType);
            }

            if (!string.IsNullOrEmpty(propertyName))
            {
                AddProviderType(collectionName, propertyName, propertyValue, isKey);
            }
        }

        private void AddProviderType(string collectionName, string elementName, BsonValue elementValue, bool isKey = false)
        {
            Type providerType = ResolveProviderType(elementValue);
            if (providerType != null)
            {
                if (providerType.IsValueType && !isKey)
                {
                    providerType = typeof (Nullable<>).MakeGenericType(providerType);
                }
                this.providerTypes.Add(string.Join(".", collectionName, elementName), providerType);
            }
        }

        private static Type ResolveProviderType(BsonValue elementValue)
        {
            if (elementValue.BsonType == BsonType.Document)
            {
                return typeof(BsonDocument);
            }
            else if (elementValue.RawValue != null)
            {
                switch (elementValue.BsonType)
                {
                    case BsonType.DateTime:
                        return typeof(DateTime);
                    default:
                        return elementValue.RawValue.GetType();
                }
            }
            return null;
        }

        public static bool IsObjectId(BsonElement element)
        {
            return element.Name == MongoMetadata.ProviderObjectIdName;
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

        internal static string GetResourcePropertyName(BsonElement element)
        {
            return element.Name == MongoMetadata.ProviderObjectIdName ? MongoMetadata.MappedObjectIdName : element.Name;
        }

        internal void UpdateResourceType(MongoContext context, ResourceType resourceType, BsonElement element)
        {
            var elementType = GetElementType(element);
            RegisterResourceProperty(context, resourceType.Name, resourceType, elementType, element, ResourceTypeKind.EntityType);
        }
    }
}
