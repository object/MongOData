

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DataServiceProvider;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Mongo.Context.Queryable
{
    public class MongoQueryProvider<TDocument> : IQueryProvider
    {
        private MongoContext _mongoContext;
        private MongoMetadata _mongoMetadata;
        private string _connectionString;
        private string _collectionName;

        public MongoQueryProvider(MongoMetadata mongoMetadata, string connectionString, string collectionName)
        {
            _mongoContext = new MongoContext(connectionString);
            _mongoMetadata = mongoMetadata;
            _connectionString = connectionString;
            _collectionName = collectionName;
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            if (ExpressionUtils.IsExpressionLinqSelect(expression))
            {
                return CreateProjectionQuery<TElement>(expression);
            }
            else
            {
                return new MongoQueryableResource<TDocument>(this, expression) as IQueryable<TElement>;
            }
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new MongoQueryableResource<TDocument>(this, expression);
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
            IMongoCollection<TDocument> mongoCollection;
            Expression mongoExpression;
            MethodInfo method;

            PrepareExecution(expression, "GetEnumerableCollection", out mongoCollection, out mongoExpression, out method);

            var resourceEnumerable = method.Invoke(this, new object[] { mongoCollection, mongoExpression }) as IEnumerable<DSPResource>;
            return resourceEnumerable.GetEnumerator() as IEnumerator<TElement>;
        }

        public object ExecuteNonQuery(Expression expression)
        {
            IMongoCollection<TDocument> mongoCollection;
            Expression mongoExpression;
            MethodInfo method;

            PrepareExecution(expression, "GetExecutionResult", out mongoCollection, out mongoExpression, out method);

            return method.Invoke(this, new object[] { mongoCollection, mongoExpression });
        }

        private void PrepareExecution(Expression expression, string methodName, out IMongoCollection<TDocument> mongoCollection, out Expression mongoExpression, out MethodInfo method)
        {
            mongoCollection = _mongoContext.Database.GetCollection<TDocument>(_collectionName);
            mongoExpression = new QueryExpressionVisitor<TDocument>(mongoCollection, _mongoMetadata).Visit(expression);

            var genericMethod = this.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            method = genericMethod.MakeGenericMethod(typeof(TDocument));
        }

        private IEnumerable<DSPResource> GetEnumerableCollection<TSource>(IMongoCollection<TDocument> mongoCollection, Expression expression)
        {
            var mongoEnumerator = mongoCollection.AsQueryable<TDocument>().Provider.CreateQuery<TDocument>(expression).GetEnumerator();
            return GetEnumerable(mongoEnumerator);
        }

        private object GetExecutionResult<TSource>(IMongoCollection<TDocument> mongoCollection, Expression expression)
        {
            return mongoCollection.AsQueryable<TDocument>().Provider.Execute(expression);
        }

        private IEnumerable<DSPResource> GetEnumerable<TSource>(IEnumerator<TSource> enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return CreateDSPResource(enumerator.Current, _collectionName);
            }
            yield break;
        }

        private DSPResource CreateDSPResource<TSource>(TSource document, string resourceName)
        {
            var typedDocument = document.ToBsonDocument();
            var resource = MongoDSPConverter.CreateDSPResource(typedDocument, _mongoMetadata, resourceName);

            if (_mongoMetadata.Configuration.UpdateDynamically)
            {
                UpdateMetadataFromResourceSet(resourceName, typedDocument);
            }

            return resource;
        }

        private void UpdateMetadataFromResourceSet(string resourceName, BsonDocument typedDocument)
        {
            var resourceType = _mongoMetadata.ResolveResourceType(resourceName);
            var collection = _mongoContext.Database.GetCollection<BsonDocument>(resourceName);
            var filter = Builders<BsonDocument>.Filter.Eq(MongoMetadata.ProviderObjectIdName, ObjectId.Parse(typedDocument.GetValue(MongoMetadata.ProviderObjectIdName).ToString()));
            var bsonDocumentsTask = collection.Find(filter).ToListAsync();
            var bsonDocuments = bsonDocumentsTask.GetAwaiter().GetResult();
            var bsonDocument = bsonDocuments.FirstOrDefault();

            foreach (var element in bsonDocument.Elements)
            {
                _mongoMetadata.RegisterResourceProperty(_mongoContext, resourceType, element);
            }
        }

        private IQueryable<TElement> CreateProjectionQuery<TElement>(Expression expression)
        {
            var callExpression = expression as MethodCallExpression;

            MethodInfo methodInfo = typeof(MongoQueryProvider<TDocument>)
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
