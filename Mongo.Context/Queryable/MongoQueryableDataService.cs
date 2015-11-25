using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataServiceProvider;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using System.Reflection;
using System.ComponentModel;

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

            var conventionPack = new ConventionPack();
            conventionPack.Add(new NamedIdMemberConvention(MongoMetadata.MappedObjectIdName));
            conventionPack.Add(new IgnoreExtraElementsConvention(true));
            ConventionRegistry.Register(collectionName, conventionPack, t => t == collectionType);

            Type genericMongoQueryableResource = typeof(MongoQueryableResource<>);
            Type constructedMongoQueryableResource = genericMongoQueryableResource.MakeGenericType(collectionType);
            object mongoQueryableResource = Activator.CreateInstance(constructedMongoQueryableResource, this.mongoMetadata, connectionString, collectionName, collectionType);

            var interceptMethod = typeof(InterceptingProvider).GetMethods().First(); //FIXME
            MethodInfo genericInterceptMethod = interceptMethod.MakeGenericMethod(typeof(DSPResource));
            var expressionVisitors = new ExpressionVisitor[]
            {
                new ResultExpressionVisitor()
            };
            IQueryable interceptProvider = genericInterceptMethod.Invoke(null, new  object[]{ mongoQueryableResource, expressionVisitors }) as IQueryable;

            //return InterceptingProvider.Intercept(
            //    new MongoQueryableResource<BsonDocument>(this.mongoMetadata, connectionString, collectionName, collectionType),
            //    new ResultExpressionVisitor());
            return interceptProvider;
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
            if (MongoMetadata.CreateDynamicTypesForComplexTypes && providerType == typeof(BsonDocument))
            {
                Type dynamicType;
                if (generatedTypes.ContainsKey(typeName))
                {
                    dynamicType = generatedTypes[typeName];
                }
                else
                {
                    var typeNameWords = typeName.Split('.');
                    Func<string, bool> criteria = x => x.StartsWith(string.Join(MongoMetadata.WordSeparator, typeNameWords) + ".");

                    dynamicType = CreateDynamicTypes(criteria, providerTypes, generatedTypes);
                    generatedTypes.Add(typeName, dynamicType);
                }
                return dynamicType;
            }
            else
            {
                return providerType;
            }
        }
    }
}
