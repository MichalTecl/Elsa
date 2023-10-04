using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mtecl.ApiClientBuilder.Abstract;

namespace Mtecl.ApiClientBuilder
{
    public class QueryAttribute : Attribute, IQueryStringParam
    {
        public QueryAttribute(string key)
        {
            Key = key;
        }

        public QueryAttribute(){}

        public string Key { get; }
    }
}
