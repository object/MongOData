using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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
            _provider = provider;
            _expression = expression;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _provider.ExecuteQuery<T>(_expression);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _provider.ExecuteQuery<T>(_expression);
        }

        public Type ElementType
        {
            get { return typeof(T); }
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
