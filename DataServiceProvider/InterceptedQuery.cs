using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace DataServiceProvider
{
    public class InterceptedQuery<T> : IOrderedQueryable<T>
    {
        private Expression _expression;
        private InterceptingProvider _provider;

        public InterceptedQuery(
           InterceptingProvider provider,
           Expression expression)
        {
            this._provider = provider;
            this._expression = expression;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this._provider.ExecuteQuery<T>(this._expression);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this._provider.ExecuteQuery<T>(this._expression);
        }

        public Type ElementType
        {
            get { return typeof(T); }
        }

        public Expression Expression
        {
            get { return this._expression; }
        }

        public IQueryProvider Provider
        {
            get { return this._provider; }
        }
    }
}
