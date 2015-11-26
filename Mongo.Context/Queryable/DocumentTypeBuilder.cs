using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using MongoDB.Bson.Serialization.Attributes;

namespace Mongo.Context.Queryable
{
    public static class DocumentTypeBuilder
    {
        private static ConstructorInfo _bsondIdAttributeConstructor = typeof(BsonIdAttribute).GetConstructor(new Type[] { });

        private static Dictionary<string, Type> _cachedTypes = new Dictionary<string, Type>();

        public static Type CompileDocumentType(Type baseType, IDictionary<string, Type> fields)
        {
            string signature = baseType.FullName + string.Join(";", fields.Select(f => f.ToString()));

            Type type = null;
            lock (_cachedTypes)
            {
                if (_cachedTypes.ContainsKey(signature))
                {
                    type = _cachedTypes[signature];
                }
            }

            if (type == null)
            {
                type = CompileDocumentTypeInternal(baseType, fields);
                lock (_cachedTypes)
                {
                    if (!_cachedTypes.ContainsKey(signature))
                    {
                        _cachedTypes.Add(signature, type);
                    }
                }
            }

            return type;
        }

        private static Type CompileDocumentTypeInternal(Type baseType, IDictionary<string, Type> fields)
        {
            TypeBuilder tb = GetTypeBuilder(baseType);
            tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            foreach (var field in fields)
            {
                CreateProperty(tb, field.Key, field.Value, field.Key == MongoMetadata.ProviderObjectIdName);
            }

            Type objectType = tb.CreateType();
            return objectType;
        }

        private static TypeBuilder GetTypeBuilder(Type baseType)
        {
            var typeSignature = "DynamicType" + Guid.NewGuid().ToString();
            var an = new AssemblyName(typeSignature);
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Mongo.Context.DynamicModule");
            TypeBuilder tb = moduleBuilder.DefineType(
                                typeSignature, 
                                TypeAttributes.Public |
                                TypeAttributes.Class |
                                TypeAttributes.AutoClass |
                                TypeAttributes.AnsiClass |
                                TypeAttributes.BeforeFieldInit |
                                TypeAttributes.AutoLayout,
                                baseType);
            return tb;
        }

        private static void CreateProperty(TypeBuilder tb, string propertyName, Type propertyType, bool markAsBsonId = false)
        {
            FieldBuilder fieldBuilder = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

            PropertyBuilder propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            if (markAsBsonId)
            {
                propertyBuilder.SetCustomAttribute(_bsondIdAttributeConstructor, new byte[] { });
            }

            MethodBuilder getPropMthdBldr = tb.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
            ILGenerator getIl = getPropMthdBldr.GetILGenerator();

            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);

            MethodBuilder setPropMthdBldr =
                tb.DefineMethod("set_" + propertyName,
                  MethodAttributes.Public |
                  MethodAttributes.SpecialName |
                  MethodAttributes.HideBySig,
                  null, new[] { propertyType });

            ILGenerator setIl = setPropMthdBldr.GetILGenerator();
            Label modifyProperty = setIl.DefineLabel();
            Label exitSet = setIl.DefineLabel();

            setIl.MarkLabel(modifyProperty);
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);

            setIl.Emit(OpCodes.Nop);
            setIl.MarkLabel(exitSet);
            setIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);
        }
    }
}
