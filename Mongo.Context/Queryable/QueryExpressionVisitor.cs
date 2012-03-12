using System;
using System.Collections.Generic;
using System.Data.Services.Providers;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using DataServiceProvider;
using FluentMongo.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using ExpressionVisitor = DataServiceProvider.ExpressionVisitor;

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
            if (m.Method == GetValueMethodInfo)
            {
                if (m.Arguments[0].Type == typeof(DSPResource) && m.Arguments[1].Type == typeof(ResourceProperty))
                {
                    var constExpression = Expression.Constant(((m.Arguments[1] as ConstantExpression).Value as ResourceProperty).Name);

                    return Expression.Call(
                        SwapParameterType(m.Arguments[0]),
                        typeof(BsonDocument).GetMethod("get_Item", new Type[] { typeof(string) }),
                        constExpression);
                }
                else
                {
                    return m;
                }
            }
            else if (m.Method.Name == "OrderBy" || m.Method.Name == "OrderByDescending")
            {
                return Expression.Call(
                    ReplaceGenericMethodType(m.Method, typeof(BsonValue)),
                    this.instanceExpression,
                    Visit(ReplaceFieldLambda(m.Arguments[1])));
            }
            else if (m.Method.GetGenericArguments().Count() > 0 && m.Method.GetGenericArguments()[0] == typeof(DSPResource))
            {
                return Expression.Call(
                    ReplaceGenericMethodType(m.Method),
                    this.instanceExpression,
                    Visit(m.Arguments[1]));
            }
            else
            {
                return base.VisitMethodCall(m);
            }
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
                return Expression.Lambda(
                    Visit(lambda.Body),
                    ReplaceLambdaParameterType(lambda));
            }
            else
            {
                return base.VisitLambda(lambda);
            }
        }

        public override Expression VisitConditional(ConditionalExpression c)
        {
            // Swallow property tests for nullability
            if (c.IfTrue is ConstantExpression && (c.IfTrue as ConstantExpression).Value == null 
                && (c.Test is BinaryExpression) && (c.Test as BinaryExpression).Method.Name == "op_Equality"
                && (c.Test as BinaryExpression).Right is ConstantExpression && ((c.Test as BinaryExpression).Right as ConstantExpression).Value == null)
            {
                return c.IfFalse;
            }
            else
            {
                return base.VisitConditional(c);
            }
        }

        private Expression SwapParameterType(Expression expression)
        {
            if (expression.Type == typeof(BsonDocument))
            {
                return Expression.Parameter(typeof(DSPResource), (expression as ParameterExpression).Name);
            }
            else if (expression.Type == typeof(DSPResource))
            {
                return Expression.Parameter(typeof(BsonDocument), (expression as ParameterExpression).Name);
            }
            else
            {
                return expression as Expression;
            }
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
    }
}
