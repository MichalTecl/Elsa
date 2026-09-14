using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;

namespace DocGen
{
    internal sealed class HtmlDocumentRenderer
    {
        internal static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("cs-CZ");

        public string Render(string title, DateTime generatedAt, IEnumerable<DocumentChapter> chapters)
        {
            var main = new XElement("main", new XElement("header", new XElement("h1", title),
                new XElement("p", "Vygenerováno " + generatedAt.ToString("d. M. yyyy HH:mm", Culture))));
            foreach (var chapter in chapters)
            {
                var section = new XElement("section", new XElement("h2", chapter.Title),
                    new XElement("p", new XAttribute("class", "description"), chapter.Text));
                if (chapter.Chart != null) section.Add(new SvgChartRenderer().Render(chapter.Chart));
                main.Add(section);
            }

            var html = new XElement("html", new XAttribute("lang", "cs"),
                new XElement("head",
                    new XElement("meta", new XAttribute("charset", "utf-8")),
                    new XElement("meta", new XAttribute("name", "viewport"), new XAttribute("content", "width=device-width, initial-scale=1")),
                    new XElement("title", title),
                    new XElement("style", @"
body { margin:0; background:#f1f5f9; color:#17283b; font:16px/1.6 Segoe UI,Arial,sans-serif; }
main { max-width:1100px; margin:40px auto; padding:0 24px; }
header { margin-bottom:28px; } h1 { margin:0; font-size:36px; } h2 { margin-top:0; }
section { background:white; border:1px solid #dce4ed; border-radius:12px; padding:28px; margin-bottom:24px; }
.description { white-space:pre-line; } figure { margin:24px 0 0; } figcaption { font-weight:600; }
.chart-scroll { overflow-x:auto; } svg { display:block; width:100%; min-width:720px; height:auto; }
.legend { display:flex; flex-wrap:wrap; gap:24px; margin:12px 0; }
.swatch { display:inline-block; width:12px; height:12px; margin-right:8px; border-radius:2px; }
table { width:100%; border-collapse:collapse; margin-top:16px; font-size:14px; }
caption { text-align:left; font-weight:600; } th,td { padding:8px; border-bottom:1px solid #e2e8f0; text-align:right; }
th:first-child,td:first-child { text-align:left; }
@media print { body { background:white; } main { margin:0; padding:0; } section { border:0; padding:0; } svg { min-width:0; } }
")), new XElement("body", main));
            return "<!DOCTYPE html>\r\n" + html.ToString();
        }
    }
}
