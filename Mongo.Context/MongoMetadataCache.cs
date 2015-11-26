

using System;
using System.Collections.Generic;
using System.Data.Services.Providers;
using System.Linq;
using DataServiceProvider;

namespace Mongo.Context
{
    internal class MongoMetadataCache
    {
        private DSPMetadata dspMetadata { get; set; }
        public Dictionary<string, Type> ProviderTypes { get; set; }
        public Dictionary<string, Type> GeneratedTypes { get; set; }

        public MongoMetadataCache(string containerName, string rootNamespace)
        {
            this.dspMetadata = new DSPMetadata(containerName, rootNamespace);
            ProviderTypes = new Dictionary<string, Type>();
            GeneratedTypes = new Dictionary<string, Type>();
        }

        public DSPMetadata CloneDSPMetadata()
        {
            return this.dspMetadata.Clone() as DSPMetadata;
        }

        public ResourceSet ResolveResourceSet(string resourceName)
        {
            return this.dspMetadata.ResourceSets.SingleOrDefault(x => x.Name == resourceName);
        }

        public void AddResourceSet(string collectionName, ResourceType resourceType)
        {
            this.dspMetadata.AddResourceSet(collectionName, resourceType);
        }

        public bool TryResolveResourceType(string resourceName, out ResourceType resourceType)
        {
            return this.dspMetadata.TryResolveResourceType(resourceName, out resourceType);
        }

        public ResourceType AddEntityType(string collectionName)
        {
            return this.dspMetadata.AddEntityType(collectionName);
        }

        public ResourceType AddComplexType(string collectionName)
        {
            return this.dspMetadata.AddComplexType(collectionName);
        }

        public void AddCollectionProperty(ResourceType resourceType, string propertyName, ResourceType propertyType)
        {
            this.dspMetadata.AddCollectionProperty(resourceType, propertyName, propertyType);
        }

        public void AddCollectionProperty(ResourceType resourceType, string propertyName, Type providerType)
        {
            this.dspMetadata.AddCollectionProperty(resourceType, propertyName, providerType);
        }

        public void AddComplexProperty(ResourceType resourceType, string propertyName, ResourceType propertyType)
        {
            this.dspMetadata.AddComplexProperty(resourceType, propertyName, propertyType);
        }

        public void AddKeyProperty(ResourceType resourceType, string propertyName, Type providerType)
        {
            this.dspMetadata.AddKeyProperty(resourceType, propertyName, providerType);
        }

        public void AddPrimitiveProperty(ResourceType resourceType, string propertyName, Type providerType)
        {
            this.dspMetadata.AddPrimitiveProperty(resourceType, propertyName, providerType);
        }
    }
}