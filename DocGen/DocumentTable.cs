using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DocGen
{
    public sealed class DocumentTable
    {
        public string Title { get; set; }
        public string Note { get; set; }
        public List<string> Columns { get; } = new List<string>();
        public List<DocumentTableRow> Rows { get; } = new List<DocumentTableRow>();
    }

    public sealed class DocumentTableRow
    {
        public bool Highlighted { get; set; }
        public List<DocumentTableCell> Cells { get; } = new List<DocumentTableCell>();
    }

    public sealed class DocumentTableCell
    {
        public string Text { get; set; }
        public string Note { get; set; }
        public bool Emphasized { get; set; }
        public string Tooltip { get; set; }
        public bool IsPositive { get; set; }
        public string LinkKey { get; set; }
        public string[] HighlightTargets { get; set; }
        public bool HighlightTrends { get; set; }
    }

    internal sealed class HtmlTableRenderer
    {
        public XElement Render(DocumentTable model)
        {
            var table = new XElement("table", new XAttribute("class", "data-table"),
                new XElement("caption", model.Title),
                new XElement("thead", new XElement("tr", model.Columns.Select(column =>
                    new XElement("th", new XAttribute("scope", "col"), column)))));
            var body = new XElement("tbody");
            foreach (var row in model.Rows)
            {
                if (row.Cells.Count != model.Columns.Count)
                    throw new System.ArgumentException("Table row must match the number of columns.");
                var element = new XElement("tr");
                if (row.Highlighted) element.SetAttributeValue("class", "highlighted");
                for (var index = 0; index < row.Cells.Count; index++)
                {
                    var cell = row.Cells[index];
                    var content = new XElement(index == 0 ? "th" : "td", cell.Text);
                    if (index == 0) content.SetAttributeValue("scope", "row");
                    if (!string.IsNullOrEmpty(cell.LinkKey)) content.SetAttributeValue("data-link-key", cell.LinkKey);
                    var targets = cell.HighlightTargets ?? (cell.LinkKey == null ? null : new[] { cell.LinkKey });
                    if (targets != null && targets.Length > 0)
                    {
                        content.SetAttributeValue("data-highlight-targets", string.Join(" ", targets));
                        content.SetAttributeValue("data-highlight-kind", cell.HighlightTrends ? "trend" : "value");
                        content.SetAttributeValue("tabindex", "0");
                    }
                    var classes = new List<string>();
                    if (cell.Emphasized) classes.Add("emphasized");
                    if (cell.IsPositive) classes.Add("positive");
                    if (classes.Count > 0) content.SetAttributeValue("class", string.Join(" ", classes));
                    if (!string.IsNullOrWhiteSpace(cell.Tooltip)) content.SetAttributeValue("title", cell.Tooltip);
                    if (!string.IsNullOrEmpty(cell.Note))
                        content.Add(new XElement("small", cell.Note));
                    element.Add(content);
                }
                body.Add(element);
            }
            table.Add(body);
            return new XElement("div", new XAttribute("class", "table-block"),
                new XElement("div", new XAttribute("class", "table-scroll"), table),
                new XElement("p", new XAttribute("class", "table-note"), model.Note));
        }
    }
}
