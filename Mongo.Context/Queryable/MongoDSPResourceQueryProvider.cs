using System;
using System.Data.Services.Providers;
using DataServiceProvider;

namespace Mongo.Context.Queryable
{
    public class MongoDSPResourceQueryProvider : DSPResourceQueryProvider
    {
        public MongoDSPResourceQueryProvider()
        {
        }

        public override ResourceType GetResourceType(object target)
        {
            if (target is DSPResource)
            {
                return (target as DSPResource).ResourceType;
            }
            else
            {
                throw new NotSupportedException("Unrecognized resource type.");
            }
        }

        public override object GetPropertyValue(object target, ResourceProperty resourceProperty)
        {
            if (target is DSPResource)
            {
                return (target as DSPResource).GetValue(resourceProperty.Name);
            }
            else
            {
                throw new NotSupportedException("Unrecognized resource type.");
            }
        }
    }
}
