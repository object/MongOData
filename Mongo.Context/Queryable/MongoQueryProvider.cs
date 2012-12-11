using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using DataServiceProvider;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;

namespace Mongo.Context.Queryable
{
    public class MongoQueryProvider : IQueryProvider
    {
        private MongoContext mongoContext;
        private MongoMetadata mongoMetadata;
        private string connectionString;
        private string collectionName;
        private Type collectionType;

        public MongoQueryProvider(MongoMetadata mongoMetadata, string connectionString, string collectionName, Type collectionType)
        {
            this.mongoContext = new MongoContext(connectionString);
            this.mongoMetadata = mongoMetadata;
            this.connectionString = connectionString;
            this.collectionName = collectionName;
            this.collectionType = collectionType;
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            if (ExpressionUtils.IsExpressionLinqSelect(expression))
            {
                return CreateProjectionQuery<TElement>(expression);
            }
            else
            {
                return new MongoQueryableResource(this, expression) as IQueryable<TElement>;
            }
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new MongoQueryableResource(this, expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }
            if (!typeof(TResult).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentException("Argument expression is not valid.");
            }
            return (TResult)Execute(expression);
        }

        public object Execute(Expression expression)
        {
            return ExecuteNonQuery(expression);
        }

        public IEnumerator<TElement> ExecuteQuery<TElement>(Expression expression)
        {
            MongoCollection mongoCollection;
            Expression mongoExpression;
            MethodInfo method;

            PrepareExecution(expression, "GetEnumerableCollection", out mongoCollection, out mongoExpression, out method);

            var resourceEnumerable = method.Invoke(this, new object[] { mongoCollection, mongoExpression }) as IEnumerable<DSPResource>;
            return resourceEnumerable.GetEnumerator() as IEnumerator<TElement>;
        }

        public object ExecuteNonQuery(Expression expression)
        {
            MongoCollection mongoCollection;
            Expression mongoExpression;
            MethodInfo method;

            PrepareExecution(expression, "GetExecutionResult", out mongoCollection, out mongoExpression, out method);

            return method.Invoke(this, new object[] { mongoCollection, mongoExpression });
        }

        private void PrepareExecution(Expression expression, string methodName, out MongoCollection mongoCollection, out Expression mongoExpression, out MethodInfo method)
        {
            mongoCollection = this.mongoContext.Database.GetCollection(collectionType, collectionName);
            mongoExpression = new QueryExpressionVisitor(mongoCollection, this.mongoMetadata, collectionType).Visit(expression);

            var genericMethod = this.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            method = genericMethod.MakeGenericMethod(collectionType);
        }

        private IEnumerable<DSPResource> GetEnumerableCollection<TSource>(MongoCollection mongoCollection, Expression expression)
        {
            var mongoEnumerator = mongoCollection.AsQueryable<TSource>().Provider.CreateQuery<TSource>(expression).GetEnumerator();
            return GetEnumerable(mongoEnumerator);
        }

        private object GetExecutionResult<TSource>(MongoCollection mongoCollection, Expression expression)
        {
            return mongoCollection.AsQueryable<TSource>().Provider.Execute(expression);
        }

        private IEnumerable<DSPResource> GetEnumerable<TSource>(IEnumerator<TSource> enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return CreateDSPResource(enumerator.Current, this.collectionName);
            }
            yield break;
        }

        private DSPResource CreateDSPResource<TSource>(TSource document, string resourceName)
        {
            var typedDocument = document.ToBsonDocument();
            var resource = MongoDSPConverter.CreateDSPResource(typedDocument, this.mongoMetadata, resourceName);

            if (this.mongoMetadata.Configuration.UpdateDynamically)
            {
                UpdateMetadataFromResourceSet(resourceName, typedDocument);
            }

            return resource;
        }

        private void UpdateMetadataFromResourceSet(string resourceName, BsonDocument typedDocument)
        {
            var resourceType = mongoMetadata.ResolveResourceType(resourceName);
            var collection = mongoContext.Database.GetCollection(resourceName);
            var query = Query.EQ(MongoMetadata.ProviderObjectIdName, ObjectId.Parse(typedDocument.GetValue(MongoMetadata.ProviderObjectIdName).ToString()));
            var bsonDocument = collection.FindOne(query);
            foreach (var element in bsonDocument.Elements)
            {
                var propertyName = MongoMetadata.GetResourcePropertyName(element);
                var resourceProperty = resourceType.Properties.SingleOrDefault(x => x.Name == propertyName);
                if (resourceProperty == null)
                {
                    mongoMetadata.RegisterResourceProperty(this.mongoContext, resourceType, element);
                }
            }
        }

        private IQueryable<TElement> CreateProjectionQuery<TElement>(Expression expression)
        {
            var callExpression = expression as MethodCallExpression;

            MethodInfo methodInfo = typeof(MongoQueryProvider)
                .GetMethod("ProcessProjection", BindingFlags.Instance | BindingFlags.NonPublic)
                .MakeGenericMethod(typeof(DSPResource), typeof(TElement));

            return
                (IQueryable<TElement>)methodInfo.Invoke(this,
                    new object[]
                        {
                            callExpression.Arguments[0],
                            ExpressionUtils.RemoveQuotes(callExpression.Arguments[1])
                        });
        }

        private IQueryable<TResultElement> ProcessProjection<TSourceElement, TResultElement>(Expression source, LambdaExpression lambda)
        {
            var dataSourceQuery = this.CreateQuery<TSourceElement>(source);
            var dataSourceQueryResults = dataSourceQuery.AsEnumerable();
            var newLambda = new ProjectionExpressionVisitor().Visit(lambda) as LambdaExpression;
            var projectionFunc = (Func<TSourceElement, TResultElement>)newLambda.Compile();

            var r = dataSourceQueryResults.FirstOrDefault();
            var u = projectionFunc(r);
            var q = dataSourceQueryResults.Select(sourceItem => projectionFunc(sourceItem));
            var z = q.FirstOrDefault();

            return dataSourceQueryResults.Select(sourceItem => projectionFunc(sourceItem)).AsQueryable();
        }
    }
}
