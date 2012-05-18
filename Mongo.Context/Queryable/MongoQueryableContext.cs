using System;
using System.Collections.Generic;
using System.Data.Services.Providers;
using System.Linq;
using System.Text;
using DataServiceProvider;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Options;

namespace Mongo.Context.Queryable
{
    public class MongoQueryableContext
    {
        public DSPQueryableContext CreateContext(DSPMetadata metadata, Dictionary<string, Type> providerTypes, string connectionString)
        {
            Func<string, IQueryable> queryProviders = x => GetQueryableCollection(connectionString, providerTypes, x);
            var dspContext = new DSPQueryableContext(metadata, queryProviders);
            return dspContext;
        }

        private IQueryable GetQueryableCollection(string connectionString, Dictionary<string, Type> providerTypes, string collectionName)
        {
            var collectionType = CreateDynamicTypeForCollection(collectionName, providerTypes);

            var conventions = new ConventionProfile();
            conventions.SetIdMemberConvention(new NamedIdMemberConvention(MongoMetadata.MappedObjectIdName));
            conventions.SetIgnoreExtraElementsConvention(new AlwaysIgnoreExtraElementsConvention());
            BsonClassMap.RegisterConventions(conventions, t => t == collectionType);

            return InterceptingProvider.Intercept(
                new MongoQueryableResource(connectionString, collectionName, collectionType),
                new ResultExpressionVisitor());
        }

        private Type CreateDynamicTypeForCollection(string collectionName, Dictionary<string, Type> providerTypes)
        {
            var fields = new Dictionary<string, Type>();
            providerTypes.Where(x => x.Key.StartsWith(collectionName + ".")).ToList()
                .ForEach(x => fields.Add(x.Key.Split('.').Last(), x.Value));
            return DocumentTypeBuilder.CompileDocumentType(typeof(object), fields);
        }
    }
}
