using System;
using System.Linq.Expressions;
using DataServiceProvider;
using MongoDB.Bson;

namespace Mongo.Context.Queryable
{
    public class ProjectionExpressionVisitor :  DSPMethodTranslatingVisitor
    {
        public override Expression VisitParameter(ParameterExpression p)
        {
            if (p.Type == typeof(BsonDocument))
            {
                return Expression.Parameter(typeof(DSPResource), p.Name);
            }
            else
            {
                return base.VisitParameter(p);
            }
        }

        public override Expression VisitUnary(UnaryExpression u)
        {
            if (u.NodeType == ExpressionType.Convert && ExpressionUtils.IsConvertWithMethod(u, "get_Item"))
            {
                var callExpression = u.Operand as MethodCallExpression;
                return Visit(Expression.Call(
                    Visit(callExpression.Object),
                    typeof(DSPResource).GetMethod("GetValue", new Type[] { typeof(string) }),
                    callExpression.Arguments));
            }
            if (u.NodeType == ExpressionType.TypeAs && u.Type == typeof(BsonDocument))
            {
                return Expression.TypeAs(u.Operand, typeof (DSPResource));
            }
            else
            {
                return base.VisitUnary(u);
            }
        }
    }
}
