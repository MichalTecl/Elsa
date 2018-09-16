using System;
using System.IO;
using System.Linq;
using System.Web;

namespace Elsa.Portal.Bundles
{
    /// <summary>
    /// Summary description for Bundle
    /// </summary>
    public class Bundle : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            var bundleNameKey = context.Request.QueryString.AllKeys.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(bundleNameKey))
            {
                throw new InvalidOperationException("Bundle key missing");
            }

            var bundleFile = context.Request.QueryString[bundleNameKey];

            var bFile = context.Request.MapPath(bundleFile);
            if (!File.Exists(bFile))
            {
                throw new InvalidOperationException("Invalid bundle file name");
            }

            var lines = File.ReadAllLines(bFile);

            foreach (var line in lines)
            {
                context.Response.Write("/*");
                context.Response.Write(line);
                context.Response.Write("*/\r\n");

                var sourcePath = context.Server.MapPath(line);
                context.Response.Write(File.ReadAllText(sourcePath));

                context.Response.Write("\r\n/*------------------------------------------------------*/\r\n\r\n");
            }

        }

        public bool IsReusable
        {
            get
            {
                return true;
            }
        }
    }
}