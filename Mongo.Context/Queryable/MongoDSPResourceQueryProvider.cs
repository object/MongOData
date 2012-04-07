using System;
using System.Collections.Generic;
using System.Data.Services.Providers;
using System.Linq;
using System.Text;
using DataServiceProvider;
using MongoDB.Bson;

namespace Mongo.Context.Queryable
{
    public class MongoDSPResourceQueryProvider : DSPResourceQueryProvider
    {
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
