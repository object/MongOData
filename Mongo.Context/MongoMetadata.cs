using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Services.Providers;
using System.Linq;
using System.Text;
using DataServiceProvider;
using MongoDB.Bson;
using MongoDB.Driver.Builders;

namespace Mongo.Context
{
    class CollectionProperty
    {
        public ResourceType CollectionType { get; set; }
        public string PropertyName { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is CollectionProperty)
            {
                var prop = obj as CollectionProperty;
                return this.CollectionType == prop.CollectionType && this.PropertyName == prop.PropertyName;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return this.CollectionType.GetHashCode() ^ this.PropertyName.GetHashCode();
        }
    }

    public class MongoMetadata
    {
        public static readonly string ProviderObjectIdName = "_id";
        public static readonly string MappedObjectIdName = "db_id";
        public static readonly Type ProviderObjectIdType = typeof(BsonObjectId);
        public static readonly Type MappedObjectIdType = typeof(string);
        public static readonly string ContainerName = "MongoContext";
        public static readonly string RootNamespace = "Mongo";
        public static readonly bool UseGlobalComplexTypeNames = false;
        internal static bool CreateDynamicTypesForComplexTypes = true;
        internal static readonly string WordSeparator = "__";
        internal static readonly string PrefixForInvalidLeadingChar = "x";

        private readonly string connectionString;
        private readonly List<CollectionProperty> unresolvedProperties = new List<CollectionProperty>();
        private static readonly Dictionary<string, MongoMetadataCache> MetadataCache = new Dictionary<string, MongoMetadataCache>();
        private readonly MongoMetadataCache instanceMetadataCache;

        public MongoConfiguration.Metadata Configuration { get; private set; }
        internal Dictionary<string, Type> ProviderTypes { get { return this.instanceMetadataCache.ProviderTypes; } }
        internal Dictionary<string, Type> GeneratedTypes { get { return this.instanceMetadataCache.GeneratedTypes; } }

        public MongoMetadata(string connectionString, MongoConfiguration.Metadata metadata = null)
        {
            this.connectionString = connectionString;
            this.Configuration = metadata ?? MongoConfiguration.Metadata.Default;

            lock (MetadataCache)
            {
                this.instanceMetadataCache = GetOrCreateMetadataCache();
            }

            using (var context = new MongoContext(connectionString))
            {
                PopulateMetadata(context);
            }
        }

        public DSPMetadata CreateDSPMetadata()
        {
            return this.instanceMetadataCache.CloneDSPMetadata();
        }

        public static void ResetDSPMetadata()
        {
            MetadataCache.Clear();
        }

        private MongoMetadataCache GetOrCreateMetadataCache()
        {
            MongoMetadataCache metadataCache;
            MetadataCache.TryGetValue(this.connectionString, out metadataCache);
            if (metadataCache == null)
            {
                metadataCache = new MongoMetadataCache(ContainerName, RootNamespace);
                MetadataCache.Add(this.connectionString, metadataCache);
            }
            return metadataCache;
        }

        public ResourceType ResolveResourceType(string resourceName, string ownerPrefix = null)
        {
            ResourceType resourceType;
            var qualifiedResourceName = string.IsNullOrEmpty(ownerPrefix) ? resourceName : MongoMetadata.GetQualifiedTypeName(ownerPrefix, resourceName);
            this.instanceMetadataCache.TryResolveResourceType(GetQualifiedPropertyName(MongoMetadata.RootNamespace, qualifiedResourceName), out resourceType);
            return resourceType;
        }

        public ResourceProperty ResolveResourceProperty(ResourceType resourceType, BsonElement element)
        {
            var propertyName = MongoMetadata.GetResourcePropertyName(element, resourceType.ResourceTypeKind);
            return ResolveResourceProperty(resourceType, propertyName);
        }

        public ResourceProperty ResolveResourceProperty(ResourceType resourceType, string propertyName)
        {
            return resourceType.Properties.SingleOrDefault(x => x.Name == propertyName);
        }

        private IEnumerable<string> GetCollectionNames(MongoContext context)
        {
            return context.Database.GetCollectionNames().Where(x => !x.StartsWith("system."));
        }

        private void PopulateMetadata(MongoContext context)
        {
            lock (this.instanceMetadataCache)
            {
                foreach (var collectionName in GetCollectionNames(context))
                {
                    var resourceSet = this.instanceMetadataCache.ResolveResourceSet(collectionName);

                    if (this.Configuration.PrefetchRows == 0)
                    {
                        if (resourceSet == null)
                        {
                            AddResourceSet(context, collectionName);
                        }
                    }
                    else
                    {
                        PopulateMetadataFromCollection(context, collectionName, resourceSet);
                    }
                }

                foreach (var prop in this.unresolvedProperties)
                {
                    var providerType = typeof (string);
                    var propertyName = NormalizeResourcePropertyName(prop.PropertyName);
                    this.instanceMetadataCache.AddPrimitiveProperty(prop.CollectionType, propertyName, providerType);
                    this.instanceMetadataCache.ProviderTypes.Add(
                        GetQualifiedPropertyName(prop.CollectionType.Name, propertyName), providerType);
                }
            }
        }

        private void PopulateMetadataFromCollection(MongoContext context, string collectionName, ResourceSet resourceSet)
        {
            var collection = context.Database.GetCollection(collectionName);
            const string naturalSort = "$natural";
            var sortOrder = this.Configuration.FetchPosition == MongoConfiguration.FetchPosition.End
                                ? SortBy.Descending(naturalSort)
                                : SortBy.Ascending(naturalSort);
            var documents = collection.FindAll().SetSortOrder(sortOrder);

            int rowCount = 0;
            foreach (var document in documents)
            {
                if (resourceSet == null)
                {
                    resourceSet = AddResourceSet(context, collectionName, document);
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

        public ResourceSet ResolveResourceSet(string resourceName)
        {
            return this.instanceMetadataCache.ResolveResourceSet(resourceName);
        }

        private ResourceSet AddResourceSet(MongoContext context, string collectionName, BsonDocument document = null)
        {
            AddDocumentType(context, collectionName, document, ResourceTypeKind.EntityType);
            return this.instanceMetadataCache.ResolveResourceSet(collectionName);
        }

        private void UpdateResourceSet(MongoContext context, ResourceSet resourceSet, BsonDocument document)
        {
            foreach (var element in document.Elements)
            {
                RegisterDocumentProperty(context, resourceSet.ResourceType, element);
            }
        }

        private ResourceType AddDocumentType(MongoContext context, string collectionName, BsonDocument document, ResourceTypeKind resourceTypeKind)
        {
            var collectionType = resourceTypeKind == ResourceTypeKind.EntityType
                                     ? this.instanceMetadataCache.AddEntityType(collectionName)
                                     : this.instanceMetadataCache.AddComplexType(collectionName);

            bool hasObjectId = false;
            if (document != null)
            {
                foreach (var element in document.Elements)
                {
                    RegisterResourceProperty(context, collectionType, element);
                    if (IsObjectId(element))
                        hasObjectId = true;
                }
            }

            if (!hasObjectId)
            {
                if (resourceTypeKind == ResourceTypeKind.EntityType)
                {
                    this.instanceMetadataCache.AddKeyProperty(collectionType, MappedObjectIdName, MappedObjectIdType);
                }
                AddProviderType(collectionName, ProviderObjectIdName, BsonObjectId.Empty, true);
            }

            if (resourceTypeKind == ResourceTypeKind.EntityType)
                this.instanceMetadataCache.AddResourceSet(collectionName, collectionType);

            return collectionType;
        }

        internal void RegisterResourceProperty(MongoContext context, ResourceType resourceType, BsonElement element)
        {
            var collectionProperty = new CollectionProperty { CollectionType = resourceType, PropertyName = element.Name };
            var resourceProperty = ResolveResourceProperty(resourceType, element);
            if (resourceProperty == null)
            {
                lock (unresolvedProperties)
                {
                    var unresolvedEarlier = unresolvedProperties.Contains(collectionProperty);
                    var resolvedNow = ResolveProviderType(element.Value, IsObjectId(element)) != null;

                    if (!unresolvedEarlier && !resolvedNow)
                        this.unresolvedProperties.Add(collectionProperty);
                    else if (unresolvedEarlier && resolvedNow)
                        this.unresolvedProperties.Remove(collectionProperty);

                    if (resolvedNow)
                    {
                        AddResourceProperty(context, resourceType.Name, resourceType, element, resourceType.ResourceTypeKind == ResourceTypeKind.EntityType);
                    }
                }
            }
        }

        private void AddResourceProperty(MongoContext context, string collectionName, ResourceType collectionType,
            BsonElement element, bool treatObjectIdAsKey = false)
        {
            var elementType = GetElementType(element, treatObjectIdAsKey);
            var propertyName = GetResourcePropertyName(element, collectionType.ResourceTypeKind);
            var propertyValue = element.Value;

            if (string.IsNullOrEmpty(propertyName))
                return;

            var isKey = false;
            if (IsObjectId(element))
            {
                if (treatObjectIdAsKey)
                    this.instanceMetadataCache.AddKeyProperty(collectionType, propertyName, elementType);
                else
                    this.instanceMetadataCache.AddPrimitiveProperty(collectionType, propertyName, elementType);
                isKey = true;
            }
            else if (elementType == typeof(BsonDocument))
            {
                AddDocumentProperty(context, collectionName, collectionType, propertyName, element);
            }
            else if (elementType == typeof(BsonArray))
            {
                RegisterArrayProperty(context, collectionType, element);
            }
            else
            {
                this.instanceMetadataCache.AddPrimitiveProperty(collectionType, propertyName, elementType);
            }

            if (!string.IsNullOrEmpty(propertyName))
            {
                AddProviderType(collectionName, IsObjectId(element) ? ProviderObjectIdName : propertyName, propertyValue, isKey);
            }
        }

        private void RegisterDocumentProperties(MongoContext context, ResourceType collectionType, BsonElement element)
        {
            var resourceName = GetResourcePropertyName(element, ResourceTypeKind.EntityType);
            var resourceType = ResolveResourceType(resourceName, collectionType.Name);
            if (resourceType == null)
            {
                AddDocumentProperty(context, collectionType.Name, collectionType, resourceName, element, true);
            }
            else
            {
                foreach (var documentElement in element.Value.AsBsonDocument.Elements)
                {
                    RegisterDocumentProperty(context, resourceType, documentElement);
                }
            }
        }

        private void RegisterDocumentProperty(MongoContext context, ResourceType resourceType, BsonElement element)
        {
            var resourceProperty = ResolveResourceProperty(resourceType, element);
            if (resourceProperty == null)
            {
                RegisterResourceProperty(context, resourceType, element);
            }
            else if ((resourceProperty.Kind & ResourcePropertyKind.ComplexType) != 0 && element.Value != BsonNull.Value)
            {
                RegisterDocumentProperties(context, resourceType, element);
            }
            else if ((resourceProperty.Kind & ResourcePropertyKind.Collection) != 0 && element.Value != BsonNull.Value)
            {
                RegisterArrayProperty(context, resourceType, element);
            }
        }

        private void AddDocumentProperty(MongoContext context, string collectionName, ResourceType collectionType, string propertyName, BsonElement element, bool isCollection = false)
        {
            var resourceType = AddDocumentType(context, GetQualifiedTypeName(collectionName, propertyName),
                                                element.Value.AsBsonDocument, ResourceTypeKind.ComplexType);
            if (isCollection && ResolveResourceProperty(collectionType, propertyName) == null)
                this.instanceMetadataCache.AddCollectionProperty(collectionType, propertyName, resourceType);
            else
                this.instanceMetadataCache.AddComplexProperty(collectionType, propertyName, resourceType);
        }

        private void RegisterArrayProperty(MongoContext context, ResourceType collectionType, BsonElement element)
        {
            var propertyName = GetResourcePropertyName(element, ResourceTypeKind.EntityType);
            var bsonArray = element.Value.AsBsonArray;
            if (bsonArray != null)
            {
                foreach (var arrayValue in bsonArray)
                {
                    if (arrayValue.AsBsonValue == BsonNull.Value)
                        continue;

                    if (arrayValue.BsonType == BsonType.Document)
                    {
                        RegisterDocumentProperties(context, collectionType, new BsonElement(element.Name, arrayValue));
                    }
                    else if (ResolveResourceProperty(collectionType, propertyName) == null)
                    {
                        // OData protocol doesn't support collections of collections
                        if (arrayValue.BsonType != BsonType.Array)
                        {
                            var mappedType = BsonTypeMapper.MapToDotNetValue(arrayValue).GetType();
                            this.instanceMetadataCache.AddCollectionProperty(collectionType, propertyName, mappedType);
                        }
                    }
                }
            }
        }

        private void AddProviderType(string collectionName, string elementName, BsonValue elementValue, bool isKey = false)
        {
            Type providerType = ResolveProviderType(elementValue, isKey);
            var qualifiedName = GetQualifiedPropertyName(collectionName, elementName);
            if (providerType != null && !this.instanceMetadataCache.ProviderTypes.ContainsKey(qualifiedName))
            {
                this.instanceMetadataCache.ProviderTypes.Add(qualifiedName, providerType);
            }
        }

        private static Type ResolveProviderType(BsonValue elementValue, bool isKey)
        {
            if (elementValue.GetType() == typeof(BsonArray) || elementValue.GetType() == typeof(BsonDocument))
            {
                return elementValue.GetType();
            }
            else if (BsonTypeMapper.MapToDotNetValue(elementValue) != null)
            {
                return GetRawValueType(elementValue, isKey);
            }
            else
            {
                return null;
            }
        }

        private static bool IsObjectId(BsonElement element)
        {
            return element.Name == MongoMetadata.ProviderObjectIdName;
        }

        private static Type GetElementType(BsonElement element, bool treatObjectIdAsKey)
        {
            if (IsObjectId(element))
            {
                if (element.Value.GetType() == ProviderObjectIdType && treatObjectIdAsKey)
                    return MappedObjectIdType;
                else
                    return GetRawValueType(element.Value, treatObjectIdAsKey);
            }
            else if (element.Value.GetType() == typeof (BsonObjectId))
            {
                return MappedObjectIdType;
            }
            else if (element.Value.GetType() == typeof(BsonArray) || element.Value.GetType() == typeof(BsonDocument))
            {
                return element.Value.GetType();
            }
            else if (BsonTypeMapper.MapToDotNetValue(element.Value) != null)
            {
                return GetRawValueType(element.Value);
            }
            else
            {
                switch (element.Value.BsonType)
                {
                    case BsonType.Null:
                        return typeof(object);
                    case BsonType.Binary:
                        return typeof(byte[]);
                    default:
                        return typeof(string);
                }
            }
        }

        private static Type GetRawValueType(BsonValue elementValue, bool isKey = false)
        {
            Type elementType;
            switch (elementValue.BsonType)
            {
                case BsonType.DateTime:
                    elementType = typeof(DateTime);
                    break;
                default:
                    elementType = BsonTypeMapper.MapToDotNetValue(elementValue).GetType();
                    break;
            }
            if (!isKey && elementType.IsValueType)
            {
                elementType = typeof(Nullable<>).MakeGenericType(elementType);
            }
            return elementType;
        }

        internal static string GetQualifiedTypeName(string collectionName, string resourceName)
        {
            return UseGlobalComplexTypeNames ? resourceName : string.Join(WordSeparator, collectionName, resourceName);
        }

        internal static string GetQualifiedTypePrefix(string ownerName)
        {
            return UseGlobalComplexTypeNames ? string.Empty : ownerName;
        }

        internal static string GetQualifiedPropertyName(string typeName, string propertyName)
        {
            return string.Join(".", typeName, propertyName);
        }

        private static string GetResourcePropertyName(BsonElement element, ResourceTypeKind resourceTypeKind)
        {
            return IsObjectId(element) && resourceTypeKind != ResourceTypeKind.ComplexType ?
                MongoMetadata.MappedObjectIdName :
                NormalizeResourcePropertyName(element.Name);
        }

        private static string NormalizeResourcePropertyName(string propertyName)
        {
            propertyName = propertyName.Trim();
            return propertyName.StartsWith("_") ? PrefixForInvalidLeadingChar + propertyName : propertyName;
        }
    }
}
