using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using DataServiceProvider;
using FluentMongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;

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
            return new MongoQueryableResource(this, expression) as IQueryable<TElement>;
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
            var mongoContext = new MongoContext(this.connectionString);
            var mongoCollection = mongoContext.Database.GetCollection<BsonDocument>(collectionName);
            var queryableCollection = mongoCollection.AsQueryable();
            var mongoExpression = new QueryExpressionVisitor(mongoCollection, queryableCollection.Expression).Visit(expression);
            var mongoEnumerator = queryableCollection.Provider.CreateQuery<BsonDocument>(mongoExpression).GetEnumerator();
            var resourceEnumerable = GetEnumerableCollection(mongoEnumerator);
            return resourceEnumerable.GetEnumerator() as IEnumerator<TElement>;
        }

        public IEnumerable<DSPResource> GetEnumerableCollection(IEnumerator<BsonDocument> enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return MongoDSPConverter.CreateDSPResource(enumerator.Current, MongoQueryableDataService.Metadata, MongoMetadata.RootNamespace, this.collectionName);
            }
            yield break;
        }
    }
}
