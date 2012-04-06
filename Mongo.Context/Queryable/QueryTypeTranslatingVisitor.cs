using System;
using System.Collections.Generic;
using System.Data.Services.Providers;
using System.Linq;
using System.Linq.Expressions;
using DataServiceProvider;
using MongoDB.Bson;

namespace Mongo.Context.Queryable
{
    public class QueryTypeTranslatingVisitor : DSPMethodTranslatingVisitor
    {
        public override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method == GetValueMethodInfo)
            {
                if (m.Arguments[0].Type == typeof(DSPResource) && m.Arguments[1].Type == typeof(ResourceProperty))
                {
                    var constExpression = Expression.Constant(((m.Arguments[1] as ConstantExpression).Value as ResourceProperty).Name);

                    return Expression.Call(
                        ExpressionUtils.ReplaceParameterType(m.Arguments[0], typeof(BsonDocument), Visit),
                        typeof(BsonDocument).GetMethod("get_Item", new Type[] { typeof(string) }),
                        constExpression);
                }
            }

            return base.VisitMethodCall(m);
        }

        public override Expression VisitUnary(UnaryExpression u)
        {
            if (u.NodeType == ExpressionType.Convert && u.Type == typeof(DSPResource))
            {
                return Expression.Convert(Visit(u.Operand), typeof (BsonDocument));
            }

            return base.VisitUnary(u);
        }
    }
}
