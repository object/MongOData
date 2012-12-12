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
        public Dictionary<string, Type> GeneratedTypes { get; set; }
    }

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
        public static readonly Type MappedObjectIdType = typeof(string);
        public static readonly string ContainerName = "MongoContext";
        public static readonly string RootNamespace = "Mongo";
        public static readonly bool UseGlobalComplexTypeNames = false;
        public static readonly bool CreateDynamicTypesForComplexTypes = true;
        internal static readonly string WordSeparator = "__";
        internal static readonly string PrefixForInvalidLeadingChar = "x";

        private string connectionString;
        private DSPMetadata dspMetadata;
        private readonly Dictionary<string, Type> providerTypes;
        private readonly Dictionary<string, Type> generatedTypes;
        private readonly List<CollectionProperty> unresolvedProperties = new List<CollectionProperty>();
        private static readonly Dictionary<string, MongoMetadataCache> MetadataCache = new Dictionary<string, MongoMetadataCache>();

        public MongoConfiguration.Metadata Configuration { get; private set; }
        public Dictionary<string, Type> ProviderTypes { get { return this.providerTypes; } }
        public Dictionary<string, Type> GeneratedTypes { get { return this.generatedTypes; } }

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
                                            ProviderTypes = new Dictionary<string, Type>(),
                                            GeneratedTypes = new Dictionary<string, Type>(),
                                        };
                    MetadataCache.Add(this.connectionString, metadataCache);
                }
                this.dspMetadata = metadataCache.DspMetadata;
                this.providerTypes = metadataCache.ProviderTypes;
                this.generatedTypes = metadataCache.GeneratedTypes;
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

            foreach (var prop in this.unresolvedProperties)
            {
                var providerType = typeof (string);
                this.dspMetadata.AddPrimitiveProperty(prop.CollectionType, prop.PropertyName, providerType);
                this.providerTypes.Add(string.Join(".", prop.CollectionType.Name, prop.PropertyName), providerType);
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
                var resourceProperty = resourceSet.ResourceType.Properties.SingleOrDefault(x => x.Name == propertyName);
                if (resourceProperty == null)
                {
                    var elementType = GetElementType(element);
                    RegisterResourceProperty(context, resourceSet.Name, resourceSet.ResourceType, elementType, element, true);
                }
                else if ((resourceProperty.Kind & ResourcePropertyKind.ComplexType) != 0 && element.Value != BsonNull.Value)
                {
                    UpdateComplexProperty(context, resourceSet, resourceProperty, element);
                }
            }
        }

        private ResourceType RegisterResourceType(MongoContext context, string collectionName, BsonDocument document, ResourceTypeKind resourceTypeKind)
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
                    RegisterResourceProperty(context, collectionName, collectionType, elementType, element, resourceTypeKind == ResourceTypeKind.EntityType);
                    if (IsObjectId(element))
                        hasObjectId = true;
                }
            }

            if (resourceTypeKind == ResourceTypeKind.EntityType)
            {
                if (!hasObjectId)
                {
                    this.dspMetadata.AddKeyProperty(collectionType, MappedObjectIdName, MappedObjectIdType);
                    AddProviderType(collectionName, ProviderObjectIdName, BsonObjectId.Empty, true);
                }

                this.dspMetadata.AddResourceSet(collectionName, collectionType);
            }

            return collectionType;
        }

        private void UpdateResourceType(MongoContext context, ResourceType collectionType, BsonDocument document)
        {
            if (document != null)
            {
                foreach (var element in document.Elements)
                {
                    var elementType = GetElementType(element);
                    if (ResolveResourceProperty(collectionType, element) == null)
                    {
                        RegisterResourceProperty(context, collectionType.Name, collectionType, elementType, element, false);
                    }
                }
            }
        }

        internal void RegisterResourceProperty(MongoContext context, ResourceType resourceType, BsonElement element)
        {
            RegisterResourceProperty(context, resourceType.Name, resourceType, GetElementType(element), element, true);
        }

        private void RegisterResourceProperty(MongoContext context, string collectionName, ResourceType collectionType,
            Type elementType, BsonElement element, bool treatObjectIdAsKey = false)
        {
            var propertyName = GetResourcePropertyName(element);
            var propertyValue = element.Value;

            var collectionProperty = new CollectionProperty {CollectionType = collectionType, PropertyName = element.Name};
            if (ResolveProviderType(element.Value, element.Name == MongoMetadata.ProviderObjectIdName) == null)
            {
                if (!unresolvedProperties.Contains(collectionProperty))
                {
                    this.unresolvedProperties.Add(collectionProperty);
                }
                return;
            }
            else if (unresolvedProperties.Contains(collectionProperty))
            {
                unresolvedProperties.Remove(collectionProperty);
            }

            var isKey = false;
            if (IsObjectId(element))
            {
                if (treatObjectIdAsKey)
                    this.dspMetadata.AddKeyProperty(collectionType, propertyName, elementType);
                else
                    this.dspMetadata.AddPrimitiveProperty(collectionType, propertyName, elementType);
                isKey = true;
            }
            else if (elementType == typeof(BsonDocument))
            {
                RegisterDocumentProperty(context, collectionName, collectionType, propertyName, element);
            }
            else if (elementType == typeof(BsonArray))
            {
                RegisterArrayProperty(context, collectionName, collectionType, propertyName, element);
            }
            else
            {
                this.dspMetadata.AddPrimitiveProperty(collectionType, propertyName, elementType);
            }

            if (!string.IsNullOrEmpty(propertyName))
            {
                AddProviderType(collectionName, propertyName == MappedObjectIdName ? ProviderObjectIdName : propertyName, propertyValue, isKey);
            }
        }

        private void UpdateComplexProperty(MongoContext context, ResourceSet resourceSet, ResourceProperty resourceProperty, BsonElement element)
        {
            var resourceName = GetResourcePropertyName(element);
            var resourceType = ResolveResourceType(resourceName, resourceSet.ResourceType.Name);
            UpdateResourceType(context, resourceType, element.Value.AsBsonDocument);
        }

        private void RegisterDocumentProperty(MongoContext context, string collectionName, ResourceType collectionType, string propertyName, BsonElement element)
        {
            ResourceType resourceType = null;
            var resourceSet = this.dspMetadata.ResourceSets.SingleOrDefault(x => x.Name == propertyName);
            if (resourceSet != null)
            {
                resourceType = resourceSet.ResourceType;
            }
            else
            {
                resourceType = RegisterResourceType(context, GetComplexTypeName(collectionName, propertyName),
                                                    element.Value.AsBsonDocument, ResourceTypeKind.ComplexType);
            }
            this.dspMetadata.AddComplexProperty(collectionType, propertyName, resourceType);
        }

        private void RegisterArrayProperty(MongoContext context, string collectionName, ResourceType collectionType, string propertyName, BsonElement element)
        {
            var bsonArray = element.Value.AsBsonArray;
            if (bsonArray != null && bsonArray.Count > 0)
            {
                var arrayElement = bsonArray.First();
                if (arrayElement.BsonType == BsonType.Document)
                {
                    ResourceType resourceType = null;
                    var resourceSet = this.dspMetadata.ResourceSets.SingleOrDefault(x => x.Name == propertyName);
                    if (resourceSet != null)
                    {
                        resourceType = resourceSet.ResourceType;
                    }
                    else
                    {
                        resourceType = RegisterResourceType(context, GetCollectionTypeName(collectionName, propertyName),
                                                            arrayElement.AsBsonDocument, ResourceTypeKind.ComplexType);
                    }
                    this.dspMetadata.AddCollectionProperty(collectionType, propertyName, resourceType);
                }
                else
                {
                    this.dspMetadata.AddCollectionProperty(collectionType, propertyName, arrayElement.RawValue.GetType());
                }
            }
        }

        private void AddProviderType(string collectionName, string elementName, BsonValue elementValue, bool isKey = false)
        {
            Type providerType = ResolveProviderType(elementValue, elementName == MongoMetadata.ProviderObjectIdName);
            if (providerType != null)
            {
                this.providerTypes.Add(string.Join(".", collectionName, elementName), providerType);
            }
        }

        private static Type ResolveProviderType(BsonValue elementValue, bool isKey)
        {
            if (elementValue.GetType() == typeof(BsonArray) || elementValue.GetType() == typeof(BsonDocument))
            {
                return elementValue.GetType();
            }
            else if (elementValue.RawValue != null)
            {
                return GetRawValueType(elementValue, isKey);
            }
            else
            {
                return null;
            }
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
                return GetRawValueType(element.Value);
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

        private static Type GetRawValueType(BsonValue elementValue, bool isKey = false)
        {
            Type elementType;
            switch (elementValue.BsonType)
            {
                case BsonType.DateTime:
                    elementType = typeof(DateTime);
                    break;
                default:
                    elementType = elementValue.RawValue.GetType();
                    break;
            }
            if (!isKey && elementType.IsValueType)
            {
                elementType = typeof(Nullable<>).MakeGenericType(elementType);
            }
            return elementType;
        }

        internal static string GetComplexTypePrefix(string ownerName)
        {
            return UseGlobalComplexTypeNames ? string.Empty : ownerName;
        }

        internal static string GetComplexTypeName(string collectionName, string resourceName)
        {
            return UseGlobalComplexTypeNames ? resourceName : string.Join(WordSeparator, collectionName, resourceName);
        }

        internal static string GetCollectionTypePrefix(string ownerName)
        {
            return UseGlobalComplexTypeNames ? string.Empty : ownerName;
        }

        internal static string GetCollectionTypeName(string collectionName, string resourceName)
        {
            return UseGlobalComplexTypeNames ? resourceName : string.Join(WordSeparator, collectionName, resourceName);
        }

        internal static string GetResourcePropertyName(BsonElement element)
        {
            return element.Name == MongoMetadata.ProviderObjectIdName ?
                MongoMetadata.MappedObjectIdName : element.Name.StartsWith("_") ?
                PrefixForInvalidLeadingChar + element.Name :
                element.Name;
        }
    }
}
