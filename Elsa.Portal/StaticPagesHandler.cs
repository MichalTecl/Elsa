using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Routing;
using Elsa.Common.Interfaces;
using Elsa.Portal.HtmlTransformations;

namespace Elsa.Portal
{
    public class StaticPagesHandler : IHttpHandler
    {
        private static readonly ConcurrentDictionary<string, string> s_transformedPages = new ConcurrentDictionary<string, string>();

        private const string ThisUserStuff = "%this_user_stuff%";

        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            var mpath = context.Server.MapPath(context.Request.Path).ToLowerInvariant();

#if (DEBUG)
            s_transformedPages.Clear();
#endif
            try
            {
                var transformed =
                    s_transformedPages.GetOrAdd(mpath, TransformScriptTags.Transform(File.ReadAllText(mpath)));

                if (transformed.Contains(ThisUserStuff))
                {
                    using (var locator = Global.Container.GetLocator())
                    {
                        var session = locator.Get<IWebSession>();

                        session.Initialize(context);

                        var urjson = string.Join(",", session.UserRights.Select(ur => $"\"{ur}\":true"));

                        transformed = transformed.Replace(ThisUserStuff, urjson);
                    }
                }                
                
                context.Response.Write(transformed);
            }
            catch (FileNotFoundException)
            {
                throw new HttpException(404, "Not found");
            }
        }

        
    }
}
