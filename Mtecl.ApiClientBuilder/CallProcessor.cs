using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Mtecl.ApiClientBuilder
{
    public static class CallProcessor
    {
        public static Task<TResult> Call<TParams, TResult>(string url, HttpMethod method, TParams parameters)
        {
            return Task.FromResult(default(TResult));
        }
    }
}
