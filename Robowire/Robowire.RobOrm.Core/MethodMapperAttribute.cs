using System;
using System.Reflection;

namespace Robowire.RobOrm.Core
{
    public sealed class MethodMapperAttribute : Attribute
    {
        public readonly Type MapperType;

        public MethodMapperAttribute(Type mapperType)
        {
            MapperType = mapperType;
        }

        public static IMethodMapper GetMapper(MethodInfo method)
        {
            var attr = Attribute.GetCustomAttribute(method, typeof(MethodMapperAttribute)) as MethodMapperAttribute;
            if (attr == null)
            {
                return null;
            }

            return Activator.CreateInstance(attr.MapperType) as IMethodMapper;
        }
    }
}
