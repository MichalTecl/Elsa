using System;
using System.IO;

using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;
using iTextSharp.tool.xml.html;
using iTextSharp.tool.xml.parser;
using iTextSharp.tool.xml.pipeline.css;
using iTextSharp.tool.xml.pipeline.end;
using iTextSharp.tool.xml.pipeline.html;

namespace Elsa.Common.Noml.Forms
{
    public static class PdfGenerator
    {
        public static void Generate(string html, string css, Stream pdfStream)
        {
            GetCalibri();

            using (var document = new Document())
            {
                var writer = PdfWriter.GetInstance(document, pdfStream);

                document.Open();
                
                var tagProcessorFactory = Tags.GetHtmlTagProcessorFactory();

                var htmlPipelineContext = new HtmlPipelineContext(null);
                htmlPipelineContext.SetTagFactory(tagProcessorFactory);

                var pdfWriterPipeline = new PdfWriterPipeline(document, writer);
                var htmlPipeline = new HtmlPipeline(htmlPipelineContext, pdfWriterPipeline);

                var cssResolver = XMLWorkerHelper.GetInstance().GetDefaultCssResolver(true);
                cssResolver.Clear();
                cssResolver.AddCss(css, "utf-8", false);
                var cssResolverPipeline = new CssResolverPipeline(cssResolver, htmlPipeline);

                var worker = new XMLWorker(cssResolverPipeline, true);
                var parser = new XMLParser(worker);
                using (var stringReader = new StringReader(html))
                {
                    parser.Parse(stringReader);
                }
            }
        }

        private static iTextSharp.text.Font GetCalibri()
        {
            var fontName = "Calibri";
            if (!FontFactory.IsRegistered(fontName))
            {
                var fontPath = Environment.GetEnvironmentVariable("SystemRoot") + "\\fonts\\calibri.ttf";
                FontFactory.Register(fontPath);
            }

            return FontFactory.GetFont(fontName, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
        }

    }
}