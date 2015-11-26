using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using DataServiceProvider;

namespace Mongo.Context.Queryable
{
    public class MongoQueryableResource<TDocument> : IQueryable<DSPResource>
    {
        private MongoMetadata mongoMetadata;
        private MongoQueryProvider<TDocument> provider;
        private Expression expression;

        public MongoQueryableResource(MongoMetadata mongoMetadata, string connectionString, string collectionName)
        {
            this.mongoMetadata = mongoMetadata;
            this.provider = new MongoQueryProvider<TDocument>(mongoMetadata, connectionString, collectionName);
            this.expression = (new DSPResource[0]).AsQueryable().Expression;
        }

        public MongoQueryableResource(MongoQueryProvider<TDocument> provider, Expression expression)
        {
            this.provider = provider;
            this.expression = expression;
        }

        public IEnumerator<DSPResource> GetEnumerator()
        {
            return this.provider.ExecuteQuery<DSPResource>(this.expression);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.provider.ExecuteQuery<DSPResource>(this.expression);
        }

        public Type ElementType
        {
            get { return typeof(DSPResource); }
        }

        public Expression Expression
        {
            get { return this.expression; }
        }

        public IQueryProvider Provider
        {
            get { return this.provider; }
        }
    }
}
