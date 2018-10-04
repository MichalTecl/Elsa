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

        public FileResult(string fileName, byte[] data)
        {
            m_fileName = fileName;
            m_data = data;
        }

        public void WriteResponse(HttpContextBase context)
        {
            var fileType = Path.GetExtension(m_fileName)?.Replace(".", "") ?? "file";

            context.Response.Clear();
            context.Response.ContentType = $"application/{fileType}";
            context.Response.AddHeader("Content-Disposition", $"attachment; filename={m_fileName}");
            context.Response.OutputStream.Write(m_data, 0, m_data.Length);
            context.Response.End();
        }
    }
}
