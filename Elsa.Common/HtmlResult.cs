using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using Elsa.Common.Noml.Core;

namespace Elsa.Common
{
    public class HtmlResult : ICustomResult
    {
        private readonly IRenderable m_renderable;

        public HtmlResult(IRenderable renderable)
        {
            m_renderable = renderable;
        }

        public void WriteResponse(HttpContextBase context)
        {
            context.Response.Clear();
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentType = "text/html; charset=UTF-8";

            m_renderable.Render(context.Response.Output);
            
            context.Response.End();
        }
    }
}
