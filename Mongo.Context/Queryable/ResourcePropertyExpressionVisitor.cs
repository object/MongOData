using System;
using System.Collections.Generic;
using System.Data.Services.Providers;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using DataServiceProvider;
using MongoDB.Bson;

namespace Mongo.Context.Queryable
{
    public class ResourcePropertyExpressionVisitor : DSPMethodTranslatingVisitor
    {
        private Dictionary<string, Type> queryFields = new Dictionary<string, Type>();

        public Dictionary<string, Type> QueryFields { get { return this.queryFields; } }
        public Type QueryDocumentType { get; set; }

        public override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.Name == "GetValue" && m.Arguments[0].NodeType == ExpressionType.MemberAccess && (m.Arguments[0] as MemberExpression).Expression.Type == typeof(ResourceProperty))
            {
                if (this.QueryDocumentType == null)
                {
                    var fieldName = (((m.Arguments[0] as MemberExpression).Expression as ConstantExpression).Value as ResourceProperty).Name;
                    Type fieldType;
                    if (!this.queryFields.TryGetValue(fieldName, out fieldType))
                    {
                        this.queryFields.Add(fieldName, typeof(BsonValue));
                    }
                }
                else
                {
                    var fieldName = (((m.Arguments[0] as MemberExpression).Expression as ConstantExpression).Value as ResourceProperty).Name;
                    var parameterExpression = Expression.Parameter(this.QueryDocumentType, (m.Object as ParameterExpression).Name);
                    var member = this.QueryDocumentType.GetMember(fieldName).First();

                    return Expression.MakeMemberAccess(parameterExpression, member);
                }
            }
            return base.VisitMethodCall(m);
        }
    }
}
