using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using DataServiceProvider;
using FluentMongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Mongo.Context.Queryable
{
    public class MongoQueryableResource : IQueryable<DSPResource>
    {
        private MongoQueryProvider provider;
        private Expression expression;

        public MongoQueryableResource(string connectionString, string collectionName)
        {
            this.provider = new MongoQueryProvider(connectionString, collectionName);
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
