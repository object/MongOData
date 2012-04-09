using System;
using System.Collections.Generic;
using System.Data.Services.Providers;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DataServiceProvider;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Mongo.Context.Queryable
{
    public class QueryExpressionVisitor : DSPMethodTranslatingVisitor
    {
        private IQueryable queryableCollection;
        private Type collectionType;

        public QueryExpressionVisitor(MongoCollection mongoCollection, Type queryDocumentType)
        {
            var genericMethod = typeof(LinqExtensionMethods).GetMethod("AsQueryable");
            var method = genericMethod.MakeGenericMethod(queryDocumentType);
            this.queryableCollection = method.Invoke(null, new object[] { mongoCollection }) as IQueryable;
            this.collectionType = queryDocumentType;
        }

        public override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.Name == "GetValue" && m.Arguments[0].NodeType == ExpressionType.MemberAccess && (m.Arguments[0] as MemberExpression).Expression.Type == typeof(ResourceProperty))
            {
                var fieldName = (((m.Arguments[0] as MemberExpression).Expression as ConstantExpression).Value as ResourceProperty).Name;
                var parameterExpression = Expression.Parameter(this.collectionType, (m.Object as ParameterExpression).Name);
                var member = this.collectionType.GetMember(fieldName).First();

                return Expression.MakeMemberAccess(parameterExpression, member);
            }
            else if (m.Method.GetGenericArguments().Any() && m.Method.GetGenericArguments()[0] == typeof(DSPResource))
            {
                if (m.Method.Name == "OrderBy" || m.Method.Name == "OrderByDescending")
                {
                    var lambda = Visit(ReplaceFieldLambda(m.Arguments[1]));
                    var method = ReplaceGenericMethodType(m.Method, (lambda as LambdaExpression).ReturnType);

                    return Visit(Expression.Call(
                        method,
                        this.queryableCollection.Expression,
                        lambda));
                }
                else if (m.Arguments.Count == 2)
                {
                    return Visit(Expression.Call(
                        ReplaceGenericMethodType(m.Method),
                        this.queryableCollection.Expression,
                        Visit(m.Arguments[1])));
                }
                else
                {
                    return Visit(Expression.Call(
                        ReplaceGenericMethodType(m.Method),
                        this.queryableCollection.Expression));
                }
            }

            return base.VisitMethodCall(m);
        }

        public override Expression VisitConstant(ConstantExpression c)
        {
            if (c.Value != null && c.Value.GetType() == typeof(EnumerableQuery<DSPResource>))
            {
                return Expression.Constant(this.queryableCollection);
            }
            else if (c.Type.IsGenericType && c.Type.BaseType == typeof(ValueType) && c.Type.UnderlyingSystemType.Name == "Nullable`1")
            {
                return Expression.Constant(c.Value);
            }

            return base.VisitConstant(c);
        }

        public override Expression VisitLambda(LambdaExpression lambda)
        {
            if (lambda.Parameters.Count > 0 && lambda.Parameters[0].Type == typeof(DSPResource))
            {
                return Visit(Expression.Lambda(
                    Visit(lambda.Body),
                    ReplaceLambdaParameterType(lambda)));
            }

            return base.VisitLambda(lambda);
        }

        public override Expression VisitMemberAccess(MemberExpression m)
        {
            //if (m.Type == typeof(Int32) && m.Member.Name == "Length" && m.Expression is MemberExpression && (m.Expression as MemberExpression).Type == typeof(string))
            //{
            //    var genericMethod = typeof (Enumerable).GetMethods().Where(x => x.Name == "Count" && x.GetParameters().Count() == 1).Single();
            //    var method = genericMethod.MakeGenericMethod(typeof (char));

            //    return Expression.Call(
            //        method,
            //        m.Expression);
            //}

            return base.VisitMemberAccess(m);
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

            return base.VisitConditional(c);
        }

        public override Expression VisitBinary(BinaryExpression b)
        {
            if (b.Left.Type == typeof(Nullable<bool>) && b.Right.Type == typeof(Nullable<bool>))
            {
                return Visit(b.Left);
            }
            
            return base.VisitBinary(b);
        }

        public override Expression VisitUnary(UnaryExpression u)
        {
            if (u.NodeType == ExpressionType.Convert)
            {
                if (u.Type.IsValueType || u.Type == typeof(string))
                {
                    return Visit(u.Operand);
                }
            }

            return base.VisitUnary(u);
        }

        private MethodInfo ReplaceGenericMethodType(MethodInfo method, params Type[] genericTypes)
        {
            var genericArguments = new List<Type>();
            genericArguments.Add(this.collectionType);
            genericArguments.AddRange(genericTypes);
            genericArguments.AddRange(method.GetGenericArguments().Skip(1 + genericTypes.Count()));

            return typeof(System.Linq.Queryable).GetMethods().Where(x => x.Name == method.Name).First()
                .MakeGenericMethod(genericArguments.ToArray());
        }

        private IEnumerable<ParameterExpression> ReplaceLambdaParameterType(LambdaExpression lambda)
        {
            var parameterExpressions = new List<ParameterExpression>();
            parameterExpressions.Add(Expression.Parameter(this.collectionType, lambda.Parameters[0].Name));
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
