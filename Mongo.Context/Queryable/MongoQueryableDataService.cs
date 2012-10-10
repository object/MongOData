using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataServiceProvider;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace Mongo.Context.Queryable
{
    public abstract class MongoQueryableDataService : MongoDataServiceBase<DSPQueryableContext, MongoDSPResourceQueryProvider>
    {
        public MongoQueryableDataService(string connectionString, MongoConfiguration mongoConfiguration = null)
            : base(connectionString, mongoConfiguration)
        {
            this.createResourceQueryProvider = () => new MongoDSPResourceQueryProvider();
        }

        public override DSPQueryableContext CreateContext(string connectionString)
        {
            Func<string, IQueryable> queryProviders = x => GetQueryableCollection(connectionString, this.mongoMetadata.ProviderTypes, x);
            var dspContext = new DSPQueryableContext(this.Metadata, queryProviders);
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
                new MongoQueryableResource(this.mongoMetadata, connectionString, collectionName, collectionType),
                new ResultExpressionVisitor());
        }

        private Type CreateDynamicTypeForCollection(string collectionName, Dictionary<string, Type> providerTypes)
        {
            var fields = new Dictionary<string, Type>();
            providerTypes.Where(x =>
                x.Key.StartsWith(collectionName + ".") || 
                MongoMetadata.UseGlobalComplexTypeNames && x.Key.StartsWith(collectionName + MongoMetadata.WordSeparator)).ToList()
                .ForEach(x => fields.Add(x.Key.Split('.').Last(), x.Value));
            return DocumentTypeBuilder.CompileDocumentType(typeof(object), fields);
        }
    }
}
