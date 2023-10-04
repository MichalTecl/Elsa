using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Mtecl.ApiClientBuilder.Helpers
{
    public class Paramlist
    {
        private readonly Dictionary<ParameterInfo, object> _values;

        public Paramlist(MethodInfo method, object[] values)
        {
            Method = method;

            var methodParams = method.GetParameters();

            _values = new Dictionary<ParameterInfo, object>();
            for (var pos = 0; pos < methodParams.Length; pos++)
                _values[methodParams[pos]] = values[pos];
        }

        public MethodInfo Method { get; }

        public IEnumerable<string> Names => _values.Keys.Select(k => k.Name);

        public bool Any() => _values.Any();

        public ParameterInfo GetParameterInfo(string paramName)
        {
            return _values.Keys.FirstOrDefault(k =>
                k.Name.Equals(paramName, StringComparison.InvariantCultureIgnoreCase));
        }

        public object GetValue(string paramName)
        {
            var k = GetParameterInfo(paramName);
            if ((k == null) || (!_values.TryGetValue(k, out var value)))
                return null;
            return value;
        }

        public object Consume(string paramName)
        {
            var k = GetParameterInfo(paramName);
            if (k == null)
                return null;

            var value = _values[k];
            _values.Remove(k);

            return value;
        }

        public T GetAttribute<T>(string paramName, T defaultVal = default(T)) where T:class
        {
            var k = GetParameterInfo(paramName);
            if (k == null)
                return defaultVal;

            return Attribute.GetCustomAttributes(k).OfType<T>().FirstOrDefault() ?? defaultVal;
        }

        public void Visit(Action<string, object> visitor)
        {
            var vlist = Names.ToList();

            foreach (var k in vlist)
            {
                if (GetParameterInfo(k) != null)
                {
                    visitor(k, GetValue(k));
                }
            }
        }
    }
}
