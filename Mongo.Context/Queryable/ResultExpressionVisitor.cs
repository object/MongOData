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
    public class ResultExpressionVisitor : DSPMethodTranslatingVisitor
    {
        public override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method == GetValueMethodInfo)
            {
                if (m.Arguments[0].Type == typeof(BsonDocument))
                {
                    return Expression.Call(
                        m.Method,
                        SwapParameterType(m.Arguments[0]),
                        Visit(m.Arguments[1]));
                }
                else if (m.Arguments[0].Type == typeof(DSPResource) && m.Arguments[1].Type == typeof(ResourceProperty))
                {
                    var constExpression = Expression.Constant(((m.Arguments[1] as ConstantExpression).Value as ResourceProperty).Name);

                    return Expression.Call(
                        SwapParameterType(m.Arguments[0]),
                        typeof(BsonDocument).GetMethod("get_Item", new Type[] { typeof(string) }),
                        constExpression);
                }
            }

            return base.VisitMethodCall(m);
        }

        private Expression SwapParameterType(Expression expression)
        {
            if (expression.Type == typeof(BsonDocument))
            {
                return Expression.Parameter(
                    typeof(DSPResource), 
                    (expression as ParameterExpression).Name);
            }
            else if (expression.Type == typeof(DSPResource))
            {
                return Expression.Parameter(
                    typeof(BsonDocument), 
                    (expression as ParameterExpression).Name);
            }
            else
            {
                return expression as Expression;
            }
        }
    }
}
