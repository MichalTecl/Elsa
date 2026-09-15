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
                if (chapter.Table != null) section.Add(new HtmlTableRenderer().Render(chapter.Table));
                main.Add(section);
            }

            var html = new XElement("html", new XAttribute("lang", "cs"),
                new XElement("head",
                    new XElement("meta", new XAttribute("charset", "utf-8")),
                    new XElement("meta", new XAttribute("name", "viewport"), new XAttribute("content", "width=device-width, initial-scale=1")),
                    new XElement("title", title),
                    new XElement("style", @"
body { margin:0; background:#f1f5f9; color:#17283b; font:16px/1.6 Segoe UI,Arial,sans-serif; }
main { max-width:1320px; margin:40px auto; padding:0 24px; }
header { margin-bottom:28px; } h1 { margin:0; font-size:36px; } h2 { margin-top:0; }
section { background:white; border:1px solid #dce4ed; border-radius:12px; padding:28px; margin-bottom:24px; }
.description { white-space:pre-line; } figure { margin:24px 0 0; } figcaption { font-weight:600; }
.chart-scroll { overflow-x:auto; } svg { display:block; width:100%; min-width:var(--chart-min-width, 720px); height:auto; }
.legend { display:flex; flex-wrap:wrap; gap:24px; margin:12px 0; }
.swatch { display:inline-block; width:12px; height:12px; margin-right:8px; border-radius:2px; }
.table-block { margin-top:28px; } .table-scroll { overflow-x:auto; }
.data-table { width:100%; border-collapse:collapse; font-size:13px; font-variant-numeric:tabular-nums; }
.data-table caption { text-align:left; font-size:16px; font-weight:600; margin-bottom:12px; }
.data-table th,.data-table td { padding:10px 12px; text-align:right; white-space:nowrap; border-bottom:1px solid #dce4ed; }
.data-table th:first-child { text-align:left; }
.data-table thead { background:#eaf0f6; }
.data-table tbody tr:nth-child(even) { background:#f8fafc; }
.data-table tbody tr.highlighted { background:#eaf5f4; }
.data-table .emphasized { font-weight:600; }
.data-table .positive { color:#287346; }
.data-table small { display:block; color:#526477; font-weight:400; font-size:11px; }
.table-note { color:#526477; font-size:13px; }
.data-table td.linked-highlight { background:#fff1a8; box-shadow:inset 0 0 0 2px #c79926; }
.data-table td.trend-highlight { background:#fff0dc; box-shadow:inset 0 0 0 2px #e38b31; }
svg rect.linked-highlight { filter:brightness(1.18); stroke:#b8780c; stroke-width:2; vector-effect:non-scaling-stroke; }
svg text.trend-highlight { fill:#ffad42; stroke:#783b00; stroke-width:0.7; }
[data-highlight-targets]:focus-visible { outline:2px solid #c79926; outline-offset:2px; }
@media print { body { background:white; } main { margin:0; padding:0; } section { border:0; padding:0; } svg { min-width:0; } }
")), new XElement("body", main, new XElement("script", DocumentHighlighting.Script)));
            return "<!DOCTYPE html>\r\n" + html.ToString();
        }
    }
}
