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
            Func<string, IQueryable> queryProviders = x => GetQueryableCollection(connectionString, x, 
                this.mongoMetadata.ProviderTypes, this.mongoMetadata.GeneratedTypes);
            var dspContext = new DSPQueryableContext(this.Metadata, queryProviders);
            return dspContext;
        }

        private IQueryable GetQueryableCollection(string connectionString, string collectionName, 
            Dictionary<string, Type> providerTypes, Dictionary<string, Type> generatedTypes)
        {
            var collectionType = CreateDynamicTypeForCollection(collectionName, providerTypes, generatedTypes);

            var conventions = new ConventionProfile();
            conventions.SetIdMemberConvention(new NamedIdMemberConvention(MongoMetadata.MappedObjectIdName));
            conventions.SetIgnoreExtraElementsConvention(new AlwaysIgnoreExtraElementsConvention());
            BsonClassMap.RegisterConventions(conventions, t => t == collectionType);

            return InterceptingProvider.Intercept(
                new MongoQueryableResource(this.mongoMetadata, connectionString, collectionName, collectionType),
                new ResultExpressionVisitor());
        }

        private Type CreateDynamicTypeForCollection(string collectionName, Dictionary<string, Type> providerTypes, Dictionary<string, Type> generatedTypes)
        {
            Func<string, bool> criteria = x =>
                                          x.StartsWith(collectionName + ".") ||
                                          MongoMetadata.UseGlobalComplexTypeNames &&
                                          x.StartsWith(collectionName + MongoMetadata.WordSeparator);

            return CreateDynamicTypes(criteria, providerTypes, generatedTypes);
        }

        private Type CreateDynamicTypes(Func<string, bool> criteria, Dictionary<string, Type> providerTypes, Dictionary<string, Type> generatedTypes)
        {
            var fieldTypes = providerTypes.Where(x => criteria(x.Key));

            var fields = fieldTypes.ToDictionary(
                x => x.Key.Split('.').Last(),
                x => GetDynamicTypeForProviderType(x.Key, x.Value, providerTypes, generatedTypes));

            return DocumentTypeBuilder.CompileDocumentType(typeof(object), fields);
        }

        private Type GetDynamicTypeForProviderType(string typeName, Type providerType, 
            Dictionary<string, Type> providerTypes, Dictionary<string, Type> generatedTypes)
        {
            if (MongoMetadata.CreateDynamicTypesForComplexTypes)
            {
                if (providerType == typeof(BsonDocument))
                {
                    Type dynamicType;
                    if (generatedTypes.ContainsKey(typeName))
                    {
                        dynamicType = generatedTypes[typeName];
                    }
                    else
                    {
                        var types = typeName.Split('.');
                        var parentName = types.First();
                        var childName = types.Last();
                        Func<string, bool> criteria = x => x.StartsWith(parentName + MongoMetadata.WordSeparator + childName);

                        dynamicType = CreateDynamicTypes(criteria, providerTypes, generatedTypes);
                        generatedTypes.Add(typeName, dynamicType);
                    }
                    return dynamicType;
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
