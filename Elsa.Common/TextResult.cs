using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Elsa.Common
{
    public class TextResult : ICustomResult
    {
        private readonly string m_content;
        private readonly string m_contentType;

        public TextResult(string content, string contentType = "application/text")
        {
            m_content = content;
            m_contentType = contentType;
        }

        public void WriteResponse(HttpContextBase context)
        {
            context.Response.Clear();
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentType = $"{m_contentType}; charset=UTF-8";

            context.Response.Output.Write(m_content);

            context.Response.End();
        }
    }
}
