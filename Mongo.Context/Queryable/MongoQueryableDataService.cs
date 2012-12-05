using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataServiceProvider;
using MongoDB.Bson;
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
            Func<string, bool> criteria = x =>
                                          x.StartsWith(collectionName + ".") ||
                                          MongoMetadata.UseGlobalComplexTypeNames &&
                                          x.StartsWith(collectionName + MongoMetadata.WordSeparator);

            return CreateDynamicTypes(providerTypes, criteria);
        }

        private Type CreateDynamicTypes(Dictionary<string, Type> providerTypes, Func<string, bool> criteria)
        {
            var fieldTypes = providerTypes.Where(x => criteria(x.Key));

            var fields = fieldTypes.ToDictionary(
                x => x.Key.Split('.').Last(),
                x => GetDynamicTypeForProviderType(x.Key, x.Value, providerTypes));

            return DocumentTypeBuilder.CompileDocumentType(typeof(object), fields);
        }

        private Type GetDynamicTypeForProviderType(string typeName, Type providerType, Dictionary<string, Type> providerTypes)
        {
            if (MongoMetadata.CreateDynamicTypesForComplexTypes)
            {
                if (providerType == typeof(BsonDocument))
                {
                    var types = typeName.Split('.');
                    var parentName = types.First();
                    var childName = types.Last();
                    Func<string, bool> criteria = x => x.StartsWith(parentName + MongoMetadata.WordSeparator + childName);

                    return CreateDynamicTypes(providerTypes, criteria);
                }
                else if (providerType == typeof(BsonArray))
                {
                    // TODO
                    return providerType;
                }
                else
                {
                    return providerType;
                }
            }
            else
            {
                return providerType;
            }
        }
    }
}
