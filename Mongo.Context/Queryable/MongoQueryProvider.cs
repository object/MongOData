using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using DataServiceProvider;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Mongo.Context.Queryable
{
    public class MongoQueryProvider : IQueryProvider
    {
        private string connectionString;
        private string collectionName;
        private Type collectionType;

        public MongoQueryProvider(string connectionString, string collectionName, Type collectionType)
        {
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
            throw new NotImplementedException();
        }

        public object Execute(Expression expression)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<TElement> ExecuteQuery<TElement>(Expression expression)
        {
            var mongoCollection = new MongoContext(this.connectionString).Database.GetCollection(collectionType, collectionName);
            var mongoExpression = new QueryExpressionVisitor(mongoCollection, collectionType).Visit(expression);
            var genericMethod = this.GetType().GetMethod("GetEnumerableCollection");
            var method = genericMethod.MakeGenericMethod(collectionType);
            var resourceEnumerable = method.Invoke(this, new object[] { mongoCollection, collectionType, mongoExpression }) as IEnumerable<DSPResource>;

            return resourceEnumerable.GetEnumerator() as IEnumerator<TElement>;
        }

        public IEnumerable<DSPResource> GetEnumerableCollection<TSource>(MongoCollection mongoCollection, Type collectionType, Expression expression)
        {
            var mongoEnumerator = mongoCollection.AsQueryable<TSource>().Provider.CreateQuery<TSource>(expression).GetEnumerator();
            return GetEnumerable(mongoEnumerator);
        }

        public IEnumerable<DSPResource> GetEnumerable<TSource>(IEnumerator<TSource> enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return MongoDSPConverter.CreateDSPResource(enumerator.Current, MongoQueryableDataService.Metadata, this.collectionName);
            }
            yield break;
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
