using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;

namespace Mongo.Context.Queryable
{
    internal static class ExpressionUtils
    {
        public static Dictionary<string, ExpressionType> VisualBasicComparisonOperators = new Dictionary<string, ExpressionType>()
                {
                    {"CompareObjectEqual", ExpressionType.Equal},
                    {"CompareObjectGreater", ExpressionType.GreaterThan},
                    {"CompareObjectGreaterEqual", ExpressionType.GreaterThanOrEqual},
                    {"CompareObjectLess", ExpressionType.LessThan},
                    {"CompareObjectLessEqual", ExpressionType.LessThanOrEqual},
                    {"CompareObjectNotEqual", ExpressionType.NotEqual},
                    {"ConditionalCompareObjectEqual", ExpressionType.Equal},
                    {"ConditionalCompareObjectGreater", ExpressionType.GreaterThan},
                    {"ConditionalCompareObjectGreaterEqual", ExpressionType.GreaterThanOrEqual},
                    {"ConditionalCompareObjectLess", ExpressionType.LessThan},
                    {"ConditionalCompareObjectLessEqual", ExpressionType.LessThanOrEqual},
                    {"ConditionalCompareObjectNotEqual", ExpressionType.NotEqual},
                };

        public static Expression RemoveQuotes(Expression expr)
        {
            while (expr.NodeType == ExpressionType.Quote)
            {
                expr = ((UnaryExpression)expr).Operand;
            }

            return expr;
        }

        public static bool IsExpressionLinqSelect(Expression expression)
        {
            return expression.NodeType == ExpressionType.Call &&
                   IsMethodLinqSelect(((MethodCallExpression)expression).Method);
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
                if (binaryExpression.NodeType == ExpressionType.Equal ||
                    binaryExpression.Method != null && binaryExpression.Method.Name == "op_Equality")
                {
                    if (binaryExpression.Right is ConstantExpression &&
                        (binaryExpression.Right as ConstantExpression).Value == null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsConvertWithMethod(Expression e, string methodName, int? argumentCount = null)
        {
            if (e is UnaryExpression && e.NodeType == ExpressionType.Convert
                && (e as UnaryExpression).Operand is MethodCallExpression &&
                ((e as UnaryExpression).Operand as MethodCallExpression).Method.Name == methodName &&
                (argumentCount == null || ((e as UnaryExpression).Operand as MethodCallExpression).Arguments.Count == argumentCount.Value))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsConvertWithVisualBasicComparison(Expression e)
        {
            foreach (var op in VisualBasicComparisonOperators)
            {
                if (IsConvertWithMethod(e, op.Key, 3) && (e.Type.IsValueType || e.Type == typeof (bool)))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsVisualBasicComparison(MethodCallExpression m)
        {
            if (m.Arguments.Count == 3)
            {
                foreach (var op in VisualBasicComparisonOperators)
                {
                    if (m.Method.Name == op.Key)
                    {
                        return true;
                    }
                }
            }
            return false;
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

        public static bool IsOrderMethod(MethodCallExpression m)
        {
            var orderMethods = new string[] {"OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending"};

            return orderMethods.Contains(m.Method.Name);
        }

        public static bool IsRedundantOrderMethod(MethodCallExpression m, LambdaExpression lambda)
        {
            if (m.Method.Name == "ThenBy" && lambda.Body.NodeType == ExpressionType.MemberAccess && lambda.ReturnType == typeof(ObjectId))
            {
                var member = lambda.Body as MemberExpression;
                return member.Expression.NodeType == ExpressionType.Parameter &&
                       (member.Expression as ParameterExpression).Name == "element" &&
                       member.Member.Name == MongoMetadata.MappedObjectIdName;
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
