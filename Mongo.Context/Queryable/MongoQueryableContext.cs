using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataServiceProvider;

namespace Mongo.Context.Queryable
{
    public class MongoQueryableContext
    {
        public DSPQueryableContext CreateContext(DSPMetadata metadata, string connectionString)
        {
            Func<string, IQueryable> queryProviders = x => GetQueryableCollection(connectionString, x);
            var dspContext = new DSPQueryableContext(queryProviders);
            return dspContext;
        }

        private IQueryable GetQueryableCollection(string connectionString, string collectionName)
        {
            return InterceptingProvider.Intercept(
                new MongoQueryableResource(connectionString, collectionName),
                new ResultTypeTranslatingVisitor());
        }
    }
}
