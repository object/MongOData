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
        internal static Expression RemoveQuotes(Expression expr)
        {
            while (expr.NodeType == ExpressionType.Quote)
            {
                expr = ((UnaryExpression)expr).Operand;
            }

            return expr;
        }

        internal static bool IsExpressionLinqSelect(Expression expression)
        {
            return expression.NodeType == ExpressionType.Call &&
                   IsMethodLinqSelect(((MethodCallExpression) expression).Method);
        }

        public static bool IsMethodLinqSelect(MethodInfo m)
        {
            return IsLinqNamedMethodSecondArgumentFunctionWithOneParameter(m, "Select");
        }

        public static bool IsMethodLinqWhere(MethodInfo m)
        {
            return IsLinqNamedMethodSecondArgumentFunctionWithOneParameter(m, "Where");
        }

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

        public static bool IsRedundantEqualityTest(ConditionalExpression c)
        {
            var constantExpression = c.IfTrue as ConstantExpression;
            if (constantExpression == null) return false;
            if (constantExpression.Value == null || constantExpression.Value.Equals(false))
            {
                var binaryExpression = c.Test as BinaryExpression;
                if (binaryExpression == null) return false;
                if (binaryExpression.NodeType == ExpressionType.Equal || binaryExpression.Method != null && binaryExpression.Method.Name == "op_Equality")
                {
                    if (binaryExpression.Right is ConstantExpression && (binaryExpression.Right as ConstantExpression).Value == null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsConvertWithMethod(Expression e, string methodName)
        {
            if (e is UnaryExpression && e.NodeType == ExpressionType.Convert
                && (e as UnaryExpression).Operand is MethodCallExpression &&
                ((e as UnaryExpression).Operand as MethodCallExpression).Method.Name == methodName)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsConvertWithMember(Expression e)
        {
            if (e is UnaryExpression && e.NodeType == ExpressionType.Convert
                && (e as UnaryExpression).Operand is MemberExpression)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static Expression ReplaceParameterType(Expression expression, Type replacementType, Func<Expression, Expression> Visit)
        {
            if (expression is ParameterExpression)
            {
                return Expression.Parameter(
                    replacementType,
                    (expression as ParameterExpression).Name);
            }
            else if (expression is UnaryExpression && expression.NodeType == ExpressionType.TypeAs)
            {
                return Expression.TypeAs(
                    Visit((expression as UnaryExpression).Operand),
                    replacementType);
            }
            return expression;
        }

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
