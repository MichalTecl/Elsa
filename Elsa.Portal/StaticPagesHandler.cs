using System;
using System.Collections.Concurrent;
using System.IO;
using System.Web;
using Elsa.Portal.HtmlTransformations;

namespace Elsa.Portal
{
    public class StaticPagesHandler : IHttpHandler
    {
        private static readonly ConcurrentDictionary<string, string> s_transformedPages = new ConcurrentDictionary<string, string>();

        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            var mpath = context.Server.MapPath(context.Request.Path).ToLowerInvariant();
            
            var transformed = s_transformedPages.GetOrAdd(mpath, TransformScriptTags.Transform(File.ReadAllText(mpath)));

            context.Response.Write(transformed);
        }

        
    }
}
