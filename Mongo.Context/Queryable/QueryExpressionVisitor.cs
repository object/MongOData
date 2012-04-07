using System;
using System.Collections.Generic;
using System.Data.Services.Providers;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DataServiceProvider;
using FluentMongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Mongo.Context.Queryable
{
    public class QueryExpressionVisitor : DSPMethodTranslatingVisitor
    {
        private MongoCollection<BsonDocument> collection;
        private Expression instanceExpression;

        public QueryExpressionVisitor(MongoCollection<BsonDocument> collection, Expression instanceExpression)
        {
            this.collection = collection;
            this.instanceExpression = instanceExpression;
        }

        public override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.Name == "GetValue" && m.Arguments[0].NodeType == ExpressionType.MemberAccess && (m.Arguments[0] as MemberExpression).Expression.Type == typeof(ResourceProperty))
            {
                var constExpression = Expression.Constant((((m.Arguments[0] as MemberExpression).Expression as ConstantExpression).Value as ResourceProperty).Name);

                return Expression.Call(
                    ExpressionUtils.ReplaceParameterType(m.Object, typeof(BsonDocument), Visit),
                    typeof(BsonDocument).GetMethod("get_Item", new Type[] { typeof(string) }),
                    constExpression);
            }
            else if (m.Method.GetGenericArguments().Any() && m.Method.GetGenericArguments()[0] == typeof(DSPResource))
            {
                if (m.Method.Name == "OrderBy" || m.Method.Name == "OrderByDescending")
                {
                    return Visit(Expression.Call(
                        ReplaceGenericMethodType(m.Method, typeof(BsonValue)),
                        this.instanceExpression,
                        Visit(ReplaceFieldLambda(m.Arguments[1]))));
                }
                else
                {
                    return Visit(Expression.Call(
                        ReplaceGenericMethodType(m.Method),
                        this.instanceExpression,
                        Visit(m.Arguments[1])));
                }
            }
            else if (m.Method.Name == "Contains" && m.Method.GetParameters().Count() == 1 && m.Method.GetParameters()[0].ParameterType == typeof(string))
            {
                if (ExpressionUtils.IsConvertWithMethod(m.Object, "get_Item"))
                {
                    return Visit(Expression.Call(
                        Visit(ReplaceContainsAccessor(m.Object)),
                        m.Method,
                        m.Arguments));
                }
            }
            return base.VisitMethodCall(m);
        }

        public override Expression VisitConstant(ConstantExpression c)
        {
            if (c.Value != null && c.Value.GetType() == typeof(EnumerableQuery<DSPResource>))
            {
                return Expression.Constant(collection.AsQueryable());
            }
            else
            {
                return base.VisitConstant(c);
            }
        }

        public override Expression VisitLambda(LambdaExpression lambda)
        {
            if (lambda.Parameters.Count > 0 && lambda.Parameters[0].Type == typeof(DSPResource))
            {
                return Visit(Expression.Lambda(
                    Visit(lambda.Body),
                    ReplaceLambdaParameterType(lambda)));
            }
            else
            {
                return base.VisitLambda(lambda);
            }
        }

        public override Expression VisitConditional(ConditionalExpression c)
        {
            if (ExpressionUtils.IsEqualityWithNullability(c))
            {
                if (ExpressionUtils.IsConvertWithMethod(c.IfFalse, "Contains"))
                {
                    return Visit((c.IfFalse as UnaryExpression).Operand);
                }
                else
                {
                    return Visit(c.IfFalse);
                }
            }
            else
            {
                return base.VisitConditional(c);
            }
        }

        public override Expression VisitBinary(BinaryExpression b)
        {
            if (b.Left.Type == typeof(Nullable<bool>) && b.Right.Type == typeof(Nullable<bool>))
            {
                return Visit(b.Left);
            }
            else
            {
                return base.VisitBinary(b);
            }
        }

        public override Expression VisitUnary(UnaryExpression u)
        {
            if (u.NodeType == ExpressionType.Convert && u.Type == typeof(DSPResource))
            {
                return Expression.Convert(Visit(u.Operand), typeof(BsonDocument));
            }

            return base.VisitUnary(u);
        }

        private MethodInfo ReplaceGenericMethodType(MethodInfo method, params Type[] genericTypes)
        {
            var genericArguments = new List<Type>();
            genericArguments.Add(typeof(BsonDocument));
            genericArguments.AddRange(genericTypes);
            genericArguments.AddRange(method.GetGenericArguments().Skip(1 + genericTypes.Count()));

            return typeof(System.Linq.Queryable).GetMethods().Where(x => x.Name == method.Name).First()
                .MakeGenericMethod(genericArguments.ToArray());
        }

        private IEnumerable<ParameterExpression> ReplaceLambdaParameterType(LambdaExpression lambda)
        {
            var parameterExpressions = new List<ParameterExpression>();
            parameterExpressions.Add(Expression.Parameter(typeof(BsonDocument)));
            parameterExpressions.AddRange(lambda.Parameters.Skip(1));

            return parameterExpressions;
        }

        private Expression ReplaceFieldLambda(Expression expression)
        {
            var lambda = (expression as UnaryExpression).Operand as LambdaExpression;

            return Expression.Lambda(
                (lambda.Body as UnaryExpression).Operand,
                lambda.Parameters);
        }

        private Expression ReplaceContainsAccessor(Expression expression)
        {
            var callExpression = (expression as UnaryExpression).Operand as MethodCallExpression;
            var fieldName = (callExpression.Arguments[0] as ConstantExpression).Value.ToString();

            var dynamicType = DocumentTypeBuilder.CompileResultType(typeof(BsonDocument), new Dictionary<string, Type>() { { fieldName, typeof(string) } });

            var parameterExpression = Expression.Parameter(dynamicType, (callExpression.Object as ParameterExpression).Name);
            var member = dynamicType.GetMember(fieldName).First();

            return Expression.MakeMemberAccess(parameterExpression, member);
        }
    }
}
