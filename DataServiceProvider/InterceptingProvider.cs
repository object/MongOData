using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace DataServiceProvider
{
    public class InterceptingProvider : IQueryProvider
    {
        private IQueryProvider _underlyingProvider;
        private Func<Expression, Expression>[] _visitors;

        private InterceptingProvider(
            IQueryProvider underlyingQueryProvider,
            params Func<Expression, Expression>[] visitors)
        {
            this._underlyingProvider = underlyingQueryProvider;
            this._visitors = visitors;
        }

        public static IQueryable<T> Intercept<T>(
            IQueryable<T> underlyingQuery,
            params ExpressionVisitor[] visitors)
        {
            Func<Expression, Expression>[] visitFuncs =
                visitors
                .Select(v => (Func<Expression, Expression>)v.Visit)
                .ToArray();
            return Intercept<T>(underlyingQuery, visitFuncs);
        }

        public static IQueryable<T> Intercept<T>(
            IQueryable<T> underlyingQuery,
            params Func<Expression, Expression>[] visitors)
        {
            InterceptingProvider provider = new InterceptingProvider(
                underlyingQuery.Provider,
                visitors
            );
            return provider.CreateQuery<T>(
                underlyingQuery.Expression);
        }

        public IEnumerator<TElement> ExecuteQuery<TElement>(
            Expression expression)
        {
            return _underlyingProvider.CreateQuery<TElement>(
                Intercept(expression)).GetEnumerator();
        }

        public IQueryable<TElement> CreateQuery<TElement>(
            Expression expression)
        {
            return new InterceptedQuery<TElement>(this, expression);
        }

        public IQueryable CreateQuery(Expression expression)
        {
            Type et = TypeHelper.FindIEnumerable(expression.Type);
            Type qt = typeof(InterceptedQuery<>).MakeGenericType(et);
            object[] args = new object[] { this, expression };

            ConstructorInfo ci = qt.GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new Type[] { 
                typeof(InterceptingProvider),
                typeof(Expression)
            },
                null);

            return (IQueryable)ci.Invoke(args);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return this._underlyingProvider.Execute<TResult>(
                Intercept(expression)
            );
        }

        public object Execute(Expression expression)
        {
            return this._underlyingProvider.Execute(
                Intercept(expression)
            );
        }

        private Expression Intercept(Expression expression)
        {
            Expression exp = expression;
            foreach (var visitor in _visitors)
                exp = visitor(exp);
            return exp;
        }
    }
}
