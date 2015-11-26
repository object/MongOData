

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using DataServiceProvider;

namespace Mongo.Context.Queryable
{
    public class MongoQueryableResource<TDocument> : IQueryable<DSPResource>
    {
        private MongoMetadata _mongoMetadata;
        private MongoQueryProvider<TDocument> _provider;
        private Expression _expression;

        public MongoQueryableResource(MongoMetadata mongoMetadata, string connectionString, string collectionName)
        {
            _mongoMetadata = mongoMetadata;
            _provider = new MongoQueryProvider<TDocument>(mongoMetadata, connectionString, collectionName);
            _expression = (new DSPResource[0]).AsQueryable().Expression;
        }

        public MongoQueryableResource(MongoQueryProvider<TDocument> provider, Expression expression)
        {
            _provider = provider;
            _expression = expression;
        }

        public IEnumerator<DSPResource> GetEnumerator()
        {
            return _provider.ExecuteQuery<DSPResource>(_expression);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _provider.ExecuteQuery<DSPResource>(_expression);
        }

        public Type ElementType
        {
            get { return typeof(DSPResource); }
        }

        public Expression Expression
        {
            get { return _expression; }
        }

        public IQueryProvider Provider
        {
            get { return _provider; }
        }
    }
}
