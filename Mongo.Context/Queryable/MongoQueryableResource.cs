using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using DataServiceProvider;

namespace Mongo.Context.Queryable
{
    public class MongoQueryableResource : IQueryable<DSPResource>
    {
        private MongoQueryProvider provider;
        private Expression expression;

        public MongoQueryableResource(string connectionString, string collectionName, Type collectionType)
        {
            this.provider = new MongoQueryProvider(connectionString, collectionName, collectionType);
            this.expression = (new DSPResource[0]).AsQueryable().Expression;
        }

        public MongoQueryableResource(MongoQueryProvider provider, Expression expression)
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
