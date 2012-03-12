using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DataServiceProvider;

namespace Mongo.Context.Queryable
{
    internal static class ExpressionUtils
    {
        /// <summary>Determines if the <paramref name="expr"/> is a call to Select.</summary>
        /// <param name="expr">Expression to inspect.</param>
        /// <returns>Instance of the <see cref="SelectCallMatch"/> class if the <paramref name="expr"/> is a Select call,
        /// or null otherwise.</returns>
        internal static SelectCallMatch MatchSelectCall(Expression expr)
        {
            if (expr.NodeType == ExpressionType.Call && IsMethodLinqSelect(((MethodCallExpression)expr).Method))
            {
                MethodCallExpression call = (MethodCallExpression)expr;
                LambdaExpression lambda = (LambdaExpression)ExpressionUtils.RemoveQuotes(call.Arguments[1]);
                Expression body = ExpressionUtils.RemoveQuotes(lambda.Body);
                return new SelectCallMatch
                {
                    MethodCall = call,
                    Source = call.Arguments[0],
                    Lambda = lambda,
                    LambdaBody = body
                };
            }
            else
            {
                return null;
            }
        }

        internal static WhereCallMatch MatchWhereCall(Expression expr)
        {
            if (expr.NodeType == ExpressionType.Call && IsMethodLinqWhere(((MethodCallExpression)expr).Method))
            {
                MethodCallExpression call = (MethodCallExpression)expr;
                LambdaExpression lambda = (LambdaExpression)ExpressionUtils.RemoveQuotes(call.Arguments[1]);
                Expression body = ExpressionUtils.RemoveQuotes(lambda.Body);
                return new WhereCallMatch
                {
                    MethodCall = call,
                    Source = call.Arguments[0],
                    Lambda = lambda,
                    LambdaBody = body
                };
            }
            else
            {
                return null;
            }
        }

        /// <summary>Returns expression stripped of any Quote expression.</summary>
        /// <param name="expr">The expression to process.</param>
        /// <returns>Expression which is guaranteed not to be a quote expression.</returns>
        internal static Expression RemoveQuotes(Expression expr)
        {
            while (expr.NodeType == ExpressionType.Quote)
            {
                expr = ((UnaryExpression)expr).Operand;
            }

            return expr;
        }

        /// <summary>Match result for a SelectCall</summary>
        public class SelectCallMatch
        {
            /// <summary>The method call expression represented by this match.</summary>
            public MethodCallExpression MethodCall { get; set; }

            /// <summary>The expression on which the Select is being called.</summary>
            public Expression Source { get; set; }

            /// <summary>The lambda expression being executed by the Select.</summary>
            public LambdaExpression Lambda { get; set; }

            /// <summary>The body of the lambda expression.</summary>
            public Expression LambdaBody { get; set; }
        }

        /// <summary>Match result for a WhereCall</summary>
        public class WhereCallMatch
        {
            /// <summary>The method call expression represented by this match.</summary>
            public MethodCallExpression MethodCall { get; set; }

            /// <summary>The expression on which the Select is being called.</summary>
            public Expression Source { get; set; }

            /// <summary>The lambda expression being executed by the Select.</summary>
            public LambdaExpression Lambda { get; set; }

            /// <summary>The body of the lambda expression.</summary>
            public Expression LambdaBody { get; set; }
        }

        /// <summary>Checks whether the specified method is the IEnumerable.Select() with Func`T,T2.</summary>
        /// <param name="m">Method to check.</param>
        /// <returns>true if this is the method; false otherwise.</returns>
        public static bool IsMethodLinqSelect(MethodInfo m)
        {
            return IsLinqNamedMethodSecondArgumentFunctionWithOneParameter(m, "Select");
        }

        public static bool IsMethodLinqWhere(MethodInfo m)
        {
            return IsLinqNamedMethodSecondArgumentFunctionWithOneParameter(m, "Where");
        }

        /// <summary>Checks whether the specified method a method on IEnumerable with Func`T,T2 parameter.</summary>
        /// <param name="m">Method to check.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <returns>true if this is the method; false otherwise.</returns>
        public static bool IsLinqNamedMethodSecondArgumentFunctionWithOneParameter(MethodInfo m, string methodName)
        {
            if (m.DeclaringType == typeof(Enumerable))
            {
                return IsNamedMethodSecondArgumentFuncWithOneParameter(m, methodName);
            }
            else
            {
                return IsNamedMethodSecondArgumentExpressionFuncWithOneParameter(m, methodName);
            }
        }

        /// <summary>Checks whether the specified method takes a Expression`Func`T1,T2 as its second argument.</summary>
        /// <param name="m">Method to check.</param>
        /// <param name="name">Expected name of method.</param>
        /// <returns>true if this is the method; false otherwise.</returns>
        private static bool IsNamedMethodSecondArgumentExpressionFuncWithOneParameter(MethodInfo m, string name)
        {
            Debug.Assert(m != null, "m != null");
            Debug.Assert(!String.IsNullOrEmpty(name), "!String.IsNullOrEmpty(name)");
            if (m.Name == name)
            {
                ParameterInfo[] p = m.GetParameters();
                if (p != null &&
                    p.Length == 2 &&
                    p[0].ParameterType.IsGenericType &&
                    p[1].ParameterType.IsGenericType)
                {
                    Type expressionParameter = p[1].ParameterType;
                    Type[] genericArgs = expressionParameter.GetGenericArguments();
                    if (genericArgs.Length == 1 && expressionParameter.GetGenericTypeDefinition() == typeof(Expression<>))
                    {
                        Type functionParameter = genericArgs[0];
                        return functionParameter.IsGenericType && functionParameter.GetGenericTypeDefinition() == typeof(Func<,>);
                    }
                }
            }

            return false;
        }

        /// <summary>Checks whether the specified method takes a Func`T1,T2 as its second argument.</summary>
        /// <param name="m">Method to check.</param>
        /// <param name="name">Expected name of method.</param>
        /// <returns>true if this is the method; false otherwise.</returns>
        private static bool IsNamedMethodSecondArgumentFuncWithOneParameter(MethodInfo m, string name)
        {
            Debug.Assert(m != null, "m != null");
            Debug.Assert(!String.IsNullOrEmpty(name), "!String.IsNullOrEmpty(name)");
            if (m.Name == name)
            {
                ParameterInfo[] p = m.GetParameters();
                if (p != null &&
                    p.Length == 2 &&
                    p[0].ParameterType.IsGenericType &&
                    p[1].ParameterType.IsGenericType)
                {
                    Type functionParameter = p[1].ParameterType;
                    return functionParameter.IsGenericType && functionParameter.GetGenericTypeDefinition() == typeof(Func<,>);
                }
            }

            return false;
        }
    }
}
