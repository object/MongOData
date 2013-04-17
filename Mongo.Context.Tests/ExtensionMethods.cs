using System;

namespace Mongo.Context.Tests
{
    public static class ExtensionMethods
    {
        public static Object CompareObjectEqual(Object left, Object right, bool caseInsensitiveTextCompare)
        {
            return left == right;
        }

        public static bool ConditionalCompareObjectEqual(Object left, Object right, bool caseInsensitiveTextCompare)
        {
            return left == right;
        }

        public static Object CompareObjectNotEqual(Object left, Object right, bool caseInsensitiveTextCompare)
        {
            return left != right;
        }

        public static bool ConditionalCompareObjectNotEqual(Object left, Object right, bool caseInsensitiveTextCompare)
        {
            return left != right;
        }
    }
}
