using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mongo.Context.Helpers
{
    static class GenericHelper
    {
        public static object Invoke(object target, Type baseType, string methodName, params Type[] genericTypes)
        {
            var genericMethod = baseType.GetMethods()
                   .Where(x => string.Compare(x.Name, methodName, true) == 0 && x.GetParameters().Single().ParameterType.IsGenericType)
                   .Single();
            var method = genericMethod.MakeGenericMethod(genericTypes);
            var retVal = method.Invoke(target, new object[] { });

            return retVal;
        }
    }
}
