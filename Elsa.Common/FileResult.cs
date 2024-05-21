using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Elsa.Common
{
    public class FileResult : ICustomResult
    {
        private readonly string m_fileName;
        private readonly byte[] m_data;
        private readonly string m_contentType;
        private readonly string m_contentDisposition;
        
        public FileResult(string fileName, byte[] data)
            : this(fileName, data, null){}

        public FileResult(string fileName, byte[] data, string contentType, string contentDisposition = "attachment")
        {
            m_fileName = fileName;
            m_data = data;
            m_contentType = contentType;
            m_contentDisposition = contentDisposition;
        }
      
        public bool AllowCrossOriginAccess { get; set; }

        public void WriteResponse(HttpContextBase context)
        {
            var fileType = Path.GetExtension(m_fileName)?.Replace(".", "") ?? "file";

            context.Response.Clear();
            context.Response.ContentType = m_contentType ?? $"application/{fileType}";
            context.Response.AddHeader("Content-Disposition", $"{m_contentDisposition}; filename={m_fileName}");

            if(AllowCrossOriginAccess)
            {
                context.Response.AddHeader("Access-Control-Allow-Origin", "*");
            }

            context.Response.OutputStream.Write(m_data, 0, m_data.Length);
            context.Response.End();
        }
    }
}
