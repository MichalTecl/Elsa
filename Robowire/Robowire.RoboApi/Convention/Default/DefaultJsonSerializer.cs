using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Routing;

using Newtonsoft.Json;

namespace Robowire.RoboApi.Convention.Default
{
    public class DefaultJsonSerializer : IParameterReader, IResultWriter
    {
        public T Read<T>(ParameterInfo parameter, RequestContext context)
        {
            if (typeof(T) == typeof(Stream))
            {
                var files = context.HttpContext.Request.Files;
                if (files.Count == 1) 
                {
                    return (T)(object)files[0].InputStream;
                }
            }

            if (typeof(T) == typeof(RequestContext))
            {
                return (T)((object)context);
            }
            
            string data = null;

            if (typeof(T).IsPrimitive || (typeof(T) == typeof(string)) || (typeof(T).GetMethod("Parse", new Type[] { typeof(string) }) != null))
            {
                var key =
                    context.RouteData.Values.Keys.FirstOrDefault(
                        i => i.Equals(parameter.Name, StringComparison.InvariantCultureIgnoreCase));
                if (key != null)
                {
                    data = context.RouteData.Values[key].ToString();
                }
            }

            if (data == null)
            {
                var queryKey =
                    context.HttpContext.Request.QueryString.AllKeys.FirstOrDefault(
                        i => !string.IsNullOrWhiteSpace(i) && i.Equals(parameter.Name, StringComparison.InvariantCultureIgnoreCase));

                if (queryKey != null)
                {
                    data = context.HttpContext.Request.QueryString[queryKey];
                }
                else
                {
                    using (var reader = new StreamReader(context.HttpContext.Request.InputStream, Encoding.UTF8))
                    {
                        data = reader.ReadToEnd();
                    }
                }
                
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)data;
                }

                return JsonConvert.DeserializeObject<T>(data);
            }
            
            if (typeof(T) == typeof(string))
            {
                return (T)(object)data;
            }

            MethodInfo parseMethod;
            if ((parseMethod = typeof(T).GetMethod("Parse", BindingFlags.Static | BindingFlags.Public)) != null)
            {
                var box = parseMethod.Invoke(null, new object[] { data });
                return (T)box;
            }

            return JsonConvert.DeserializeObject<T>(data);
        }

        public void WriteResult(MethodInfo controllerMethod, RequestContext context, object returnValue, bool isVoid)
        {
            var data = isVoid ? "{\"ok\":1}" : JsonConvert.SerializeObject(returnValue);

            context.HttpContext.Response.Write(data);
        }
    }
}
