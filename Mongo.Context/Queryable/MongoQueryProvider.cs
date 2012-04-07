using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using DataServiceProvider;
using FluentMongo.Linq;
using MongoDB.Bson;

namespace Mongo.Context.Queryable
{
    public class MongoQueryProvider : IQueryProvider
    {
        private string connectionString;
        private string collectionName;

        public MongoQueryProvider(string connectionString, string collectionName)
        {
            this.connectionString = connectionString;
            this.collectionName = collectionName;
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
            throw new NotImplementedException();
        }

        public object Execute(Expression expression)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<TElement> ExecuteQuery<TElement>(Expression expression)
        {
            var mongoCollection = new MongoContext(this.connectionString).Database.GetCollection<BsonDocument>(collectionName);
            var mongoExpression = new QueryExpressionVisitor(mongoCollection).Visit(expression);
            var mongoEnumerator = mongoCollection.AsQueryable().Provider.CreateQuery<BsonDocument>(mongoExpression).GetEnumerator();
            var resourceEnumerable = GetEnumerableCollection(mongoEnumerator);

            return resourceEnumerable.GetEnumerator() as IEnumerator<TElement>;
        }

        public IEnumerable<DSPResource> GetEnumerableCollection(IEnumerator<BsonDocument> enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return MongoDSPConverter.CreateDSPResource(enumerator.Current, MongoQueryableDataService.Metadata, this.collectionName);
            }
            yield break;
        }

        private IQueryable<TElement> CreateProjectionQuery<TElement>(Expression expression)
        {
            //var projectionExpression = new ProjectionExpressionVisitor().Visit(expression);
            //var callExpression = projectionExpression as MethodCallExpression;
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
