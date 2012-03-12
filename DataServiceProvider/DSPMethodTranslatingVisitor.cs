//*********************************************************
//
//    Copyright (c) Microsoft. All rights reserved.
//    This code is licensed under the Microsoft Public License.
//    THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//    ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//    IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//    PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

namespace DataServiceProvider
{
    using System;
    using System.Collections.Generic;
    using System.Data.Services.Providers;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>Expression visitor which translates calls to methods on the <see cref="DataServiceProviderMethods"/> class
    /// into expressions which can be evaluated by LINQ to Objects.</summary>
    public class DSPMethodTranslatingVisitor : ExpressionVisitor
    {
        /// <summary>MethodInfo for object DataServiceProviderMethods.GetValue(this object value, ResourceProperty property).</summary>
        internal protected static readonly MethodInfo GetValueMethodInfo = typeof(DataServiceProviderMethods).GetMethod(
            "GetValue",
            BindingFlags.Static | BindingFlags.Public,
            null,
            new Type[] { typeof(object), typeof(ResourceProperty) },
            null);

        /// <summary>MethodInfo for IEnumerable&lt;T&gt; DataServiceProviderMethods.GetSequenceValue(this object value, ResourceProperty property).</summary>
        internal protected static readonly MethodInfo GetSequenceValueMethodInfo = typeof(DataServiceProviderMethods).GetMethod(
            "GetSequenceValue",
            BindingFlags.Static | BindingFlags.Public,
            null,
            new Type[] { typeof(object), typeof(ResourceProperty) },
            null);

        /// <summary>MethodInfo for Convert.</summary>
        internal protected static readonly MethodInfo ConvertMethodInfo = typeof(DataServiceProviderMethods).GetMethod(
            "Convert",
            BindingFlags.Static | BindingFlags.Public);

        /// <summary>MethodInfo for TypeIs.</summary>
        internal protected static readonly MethodInfo TypeIsMethodInfo = typeof(DataServiceProviderMethods).GetMethod(
            "TypeIs",
            BindingFlags.Static | BindingFlags.Public);

        /// <summary>
        /// MethodCallExpression visit method
        /// </summary>
        /// <param name="m">The MethodCallExpression expression to visit</param>
        /// <returns>The visited MethodCallExpression expression </returns>
        public override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method == GetValueMethodInfo)
            {
                // Arguments[0] - the resource to get property value of - we assume it's a DSPEntity
                // Arguments[1] - the ResourceProperty to get value of

                // Just call the targetResource.GetValue(resourceProperty.Name)
                return Expression.Call(
                    this.Visit(m.Arguments[0]),
                    typeof(DSPResource).GetMethod("GetValue"),
                    Expression.Property(m.Arguments[1], "Name"));
            }
            else if (m.Method.IsGenericMethod && m.Method.GetGenericMethodDefinition() == GetSequenceValueMethodInfo)
            {
                // Arguments[0] - the resource to get property value of - we assume it's a DSPEntity
                // Arguments[1] - the ResourceProperty to get value of

                // Just call the targetResource.GetValue(resourceProperty.Name) and cast it to the right IEnumerable<T> (which is the return type of the GetSequenceMethod
                return Expression.Convert(
                    Expression.Call(
                        this.Visit(m.Arguments[0]),
                        typeof(DSPResource).GetMethod("GetValue"),
                        Expression.Property(m.Arguments[1], "Name")),
                    m.Method.ReturnType);
            }
            else if (m.Method == ConvertMethodInfo)
            {
                // All our resources are of the same underlying CLR type, so no need for conversion of the CLR type
                //   and we don't have any specific action to take to convert the Resource Types either (as we access properties from a property bag)
                // So get rid of the conversions as we don't need them
                return this.Visit(m.Arguments[0]);
            }
            else if (m.Method == TypeIsMethodInfo)
            {
                // Arguments[0] - the resource to determine the type of - we assume it's a DSPEntity
                // Arguments[1] - the ResourceType to test for

                // We don't support type inheritance yet, so simple comparison is enough
                return Expression.Equal(Expression.Property(this.Visit(m.Arguments[0]), typeof(DSPResource).GetProperty("ResourceType")), m.Arguments[1]);
            }

            return base.VisitMethodCall(m);
        }
    }
}
