using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private MongoMetadata mongoMetadata;

        public QueryExpressionVisitor(MongoCollection mongoCollection, MongoMetadata mongoMetadata, Type queryDocumentType)
        {
            var genericMethod = typeof(LinqExtensionMethods).GetMethods()
                .Where(x => x.Name == "AsQueryable" && x.GetParameters().Single().ParameterType.IsGenericType)
                .Single();
            var method = genericMethod.MakeGenericMethod(queryDocumentType);
            this.queryableCollection = method.Invoke(null, new object[] { mongoCollection }) as IQueryable;
            this.collectionType = queryDocumentType;
            this.mongoMetadata = mongoMetadata;
        }

        public override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.Name == "GetValue" && m.Arguments[0].NodeType == ExpressionType.MemberAccess &&
                (m.Arguments[0] as MemberExpression).Expression.Type == typeof(ResourceProperty))
            {
                return ReplaceMemberAccess(m);
            }
            else if (m.Method.GetGenericArguments().Any() && m.Method.GetGenericArguments()[0] == typeof(DSPResource))
            {
                if (ExpressionUtils.IsOrderMethod(m))
                {
                    var lambda = Visit(ReplaceFieldLambda(m.Arguments[1]));

                    if (ExpressionUtils.IsRedundantOrderMethod(m, lambda as LambdaExpression))
                    {
                        return Visit(m.Arguments[0]);
                    }
                    else
                    {
                        return Visit(Expression.Call(
                            ReplaceGenericMethodType(m.Method, (lambda as LambdaExpression).ReturnType),
                            Visit(m.Arguments[0]),
                            lambda));
                    }
                }
                else
                {
                    return Visit(Expression.Call(
                        ReplaceGenericMethodType(m.Method),
                        new ReadOnlyCollection<Expression>(m.Arguments.Select(x => Visit(x)).ToList())));
                }
            }

            return base.VisitMethodCall(m);
        }

        public override Expression VisitMemberAccess(MemberExpression m)
        {
            if (m.Member.Name == "Value" && m.Member.DeclaringType == typeof(Nullable<bool>))
            {
                return ((m.Expression as ConditionalExpression).IfFalse as UnaryExpression).Operand;
            }

            return base.VisitMemberAccess(m);
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

        public override Expression VisitConditional(ConditionalExpression c)
        {
            if (ExpressionUtils.IsRedundantEqualityTest(c))
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
            if (b.Left.Type == typeof(Nullable<bool>) && b.Right.Type == typeof(Nullable<bool>) && b.NodeType == ExpressionType.Equal &&
                (b.Right.NodeType == ExpressionType.Convert || b.Right.NodeType == ExpressionType.Constant))
            {
                return Visit(ReplaceBinaryComparison(b));
            }
            else
            {
                Expression left = this.Visit(b.Left);
                Expression right = this.Visit(b.Right);
                if (left.Type == typeof(ObjectId) &&
                    right.Type == typeof(string) && right.NodeType == ExpressionType.Constant)
                {
                    return Visit(ReplaceObjectIdComparison(b.NodeType, right, left));
                }
                else if (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(Nullable<>) && left.NodeType == ExpressionType.MemberAccess &&
                    right.Type.IsValueType && right.NodeType == ExpressionType.Constant)
                {
                    return Visit(ReplaceNullableMemberComparison(b.NodeType, right, left));
                }
                else
                {
                    return base.VisitBinary(b);
                }
            }
        }

        public override Expression VisitUnary(UnaryExpression u)
        {
            if (u.NodeType == ExpressionType.Convert)
            {
                if (u.Operand.NodeType != ExpressionType.Constant && (u.Type.IsValueType || u.Type == typeof(string)))
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

            return typeof(System.Linq.Queryable).GetMethods().First(x => x.Name == method.Name)
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

        private Expression ReplaceBinaryComparison(BinaryExpression b)
        {
            var consantExpression = (b.Right is ConstantExpression ? b.Right : (b.Right as UnaryExpression).Operand) as ConstantExpression;
            if (consantExpression.Type == typeof(bool))
            {
                if ((bool)consantExpression.Value)
                    return b.Left;
                else
                    return Expression.MakeUnary(ExpressionType.Not, b.Left, b.Type);
            }
            else // null
            {
                return b.Left;
            }
        }

        private Expression ReplaceObjectIdComparison(ExpressionType nodeType, Expression right, Expression left)
        {
            right = Expression.Constant(ObjectId.Parse((right as ConstantExpression).Value.ToString()));
            return Expression.MakeBinary(nodeType, left, right);
        }

        private Expression ReplaceNullableMemberComparison(ExpressionType nodeType, Expression right, Expression left)
        {
            right = Expression.MakeUnary(ExpressionType.Convert, right, typeof(Nullable<>).MakeGenericType(right.Type));
            return Expression.MakeBinary(nodeType, left, right);
        }

        private Expression ReplaceMemberAccess(MethodCallExpression m)
        {
            var fieldName = (((m.Arguments[0] as MemberExpression).Expression as ConstantExpression).Value as ResourceProperty).Name;
            if (fieldName == MongoMetadata.MappedObjectIdName)
                fieldName = MongoMetadata.ProviderObjectIdName;

            if (m.Object.NodeType == ExpressionType.Parameter)
            {
                var parameterExpression = Expression.Parameter(this.collectionType, (m.Object as ParameterExpression).Name);
                var member = this.collectionType.GetMember(fieldName).Single();
                return Expression.MakeMemberAccess(parameterExpression, member);
            }
            else if (m.Object.NodeType == ExpressionType.Convert && (m.Object as UnaryExpression).Operand.NodeType == ExpressionType.Call)
            {
                var methodCallExpression = (m.Object as UnaryExpression).Operand as MethodCallExpression as MethodCallExpression;
                var resourceProperty = ((methodCallExpression.Arguments.First() as MemberExpression).Expression as ConstantExpression).Value as ResourceProperty;
                var typeName = resourceProperty.ResourceType.Name;
                if (!MongoMetadata.UseGlobalComplexTypeNames)
                {
                    typeName = typeName.Replace(MongoMetadata.WordSeparator, ".");
                }
                var propertyType = this.mongoMetadata.GeneratedTypes.Single(x => x.Key == typeName).Value;
                var member = propertyType.GetMember(fieldName).Single();
                var expression = ReplaceMemberAccess(methodCallExpression);
                return Expression.MakeMemberAccess(expression, member);
            }
            else
            {
                return m;
            }
        }
    }
}
