using System;
using System.Linq;
using System.Reflection;

namespace Robowire.RoboApi.Convention.Default
{
    public static class ReflectonWrapper
    {
        public static MethodInfo GetMethodInfo(Type owner, string methodName)
        {
            var methods = owner.GetMethods().Where(m => m.Name == methodName).ToArray();
            if (methods.Length != 1)
            {
                throw new InvalidOperationException($"Controller method {owner.Name}.{methodName} cannot act as a controller method because it does not exist or have overloads");
            }

            return methods[0];
        }

        public static ParameterInfo GetParameterInfo(Type owner, string methodName, string paramName)
        {
            var method = GetMethodInfo(owner, methodName);

            return method.GetParameters().Single(p => p.Name == paramName);
        }
    }
}
