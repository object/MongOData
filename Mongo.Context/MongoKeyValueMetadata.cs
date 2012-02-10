using System;
using System.Collections.Generic;
using System.Data.Services.Providers;
using System.Linq;
using System.Text;
using DataServiceProvider;

namespace Mongo.Context
{
    public class MongoKeyValueMetadata : MongoMetadataBase
    {
        protected override void PopulateMetadata(DSPMetadata metadata, MongoContext context)
        {
            var itemsType = new ResourceType(typeof(Dictionary<string, object>),
                ResourceTypeKind.ComplexType, null, "Mongo", "Items", false);

            foreach (var collectionName in GetCollectionNames(context))
            {
                var collectionType = metadata.AddEntityType(collectionName);
                metadata.AddKeyProperty(collectionType, "Id", typeof(string));
                metadata.AddComplexProperty(collectionType, "Items", itemsType);
                metadata.AddResourceSet(collectionName, collectionType);
            }
        }
    }
}
