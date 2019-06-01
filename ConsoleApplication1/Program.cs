using System;
using System.IO;
using System.Text;

using Elsa.Core.Entities.Commerce;

using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;

using Robowire;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            var css = "table ";
            var html =File.ReadAllText("C:\\Elsa\\output.html"); 
            var document = new Document(PageSize.A4);
            
            PdfWriter writer = null;
            try
            {
                if (File.Exists("mypdf.pdf"))
                {
                    File.Delete("mypdf.pdf");
                }

                writer = PdfWriter.GetInstance(document, File.OpenWrite("mypdf.pdf"));
                //writer.CloseStream = false;
                document.Open();

                using (var htmlStream = new MemoryStream(Encoding.UTF8.GetBytes(html)))
                {
                    XMLWorkerHelper.GetInstance().ParseXHtml(writer, document, htmlStream, Encoding.UTF8);
                }
            }
            finally
            {
                try
                {
                    writer?.Dispose();
                }
                catch
                {
                    ;
                }
            }
            
        }
    }
}
