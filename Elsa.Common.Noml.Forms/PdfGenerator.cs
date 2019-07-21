using System.IO;

namespace Elsa.Common.Noml.Forms
{
    public static class PdfGenerator
    {
        private const string c_head = "<head><meta charset=\"UTF-8\"></head>";

        public static byte[] Generate(string html, string css)
        {
            var doc = $"{c_head} {html} <style>{css}</style>";

            var htmlToPdf = new NReco.PdfGenerator.HtmlToPdfConverter();
            return htmlToPdf.GeneratePdf(doc);
        }
    }
}