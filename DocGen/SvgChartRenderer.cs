using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DocGen
{
    internal sealed class SvgChartRenderer
    {
        private static readonly XNamespace _svg = "http://www.w3.org/2000/svg";
        private const double LEFT = 115;
        private const double PLOT_HEIGHT = 280;
        private const double BASELINE = 304;

        public XElement Render(GroupedStackedChart chart)
        {
            Validate(chart);
            var figure = new XElement("figure", new XElement("figcaption", chart.Title));
            if (chart.Groups.Count == 0)
            {
                figure.Add(new XElement("p", "Pro graf nejsou dostupná data."));
                return figure;
            }

            var width = Math.Max(960, chart.Groups.Count * 90 + LEFT + 20);
            var groupWidth = (width - LEFT - 20) / chart.Groups.Count;
            var maximum = chart.Groups.SelectMany(g => g.Columns).Max(c => c.Values.Sum() + (c.ProjectedAdditions?.Sum() ?? 0));
            var step = NiceStep(maximum / 4);
            var axisMax = Math.Max(step, Math.Ceiling(maximum / step) * step);
            var svg = new XElement(_svg + "svg", new XAttribute("viewBox", $"0 0 {Number(width)} 390"),
                new XAttribute("style", $"--chart-min-width:{Number(width)}px"),
                new XAttribute("role", "img"), new XAttribute("aria-label", chart.Title ?? "Graf"),
                new XElement(_svg + "title", chart.Title),
                new XElement(_svg + "desc", "Syté části představují skutečné hodnoty, světlé části odhad přírůstku do konce období."));

            for (decimal value = 0; value <= axisMax; value += step)
            {
                var y = BASELINE - (double)(value / axisMax) * PLOT_HEIGHT;
                svg.Add(new XElement(_svg + "line", Attr("x1", LEFT), Attr("x2", width - 20),
                    Attr("y1", y), Attr("y2", y), new XAttribute("stroke", "#e2e8f0")));
                svg.Add(Text(LEFT - 10, y + 4, Format(value, chart.Unit), "end"));
            }

            for (var index = 0; index < chart.Groups.Count; index++)
            {
                var group = chart.Groups[index];
                var barWidth = Math.Min(38, groupWidth / group.Columns.Count * 0.8);
                var groupLeft = LEFT + index * groupWidth + (groupWidth - barWidth * group.Columns.Count) / 2;
                for (var columnIndex = 0; columnIndex < group.Columns.Count; columnIndex++)
                {
                    var column = group.Columns[columnIndex];
                    var center = groupLeft + barWidth * (columnIndex + 0.5);
                    var y = BASELINE;
                    for (var segmentIndex = 0; segmentIndex < chart.Segments.Count; segmentIndex++)
                    {
                        var segment = chart.Segments[segmentIndex];
                        var value = column.Values[segmentIndex];
                        var height = (double)(value / axisMax) * PLOT_HEIGHT;
                        y -= height;
                        svg.Add(new XElement(_svg + "rect", Attr("x", center - barWidth / 2), Attr("y", y),
                            Attr("width", barWidth), Attr("height", height), new XAttribute("fill", segment.Color), SegmentLink(column.SegmentKeys?[segmentIndex]),
                            new XElement(_svg + "title", $"{group.Label} {column.Label}, {segment.Label}: {Format(value, chart.Unit)}")));
                        var label = column.SegmentLabels?[segmentIndex];
                        if (!string.IsNullOrWhiteSpace(label) && height > 0)
                        {
                            var labelElement = Text(center, y + height / 2, label);
                            if (column.SegmentKeys != null)
                                labelElement.SetAttributeValue("data-trend-key", column.SegmentKeys[segmentIndex]);
                            labelElement.SetAttributeValue("dominant-baseline", "central");
                            labelElement.SetAttributeValue("font-size", Number(Math.Min(10, height * 0.8)));
                            labelElement.SetAttributeValue("font-weight", "600");
                            labelElement.SetAttributeValue("fill", "#ffffff");
                            labelElement.SetAttributeValue("stroke", "#17283b");
                            labelElement.SetAttributeValue("stroke-width", "0.5");
                            labelElement.SetAttributeValue("paint-order", "stroke");
                            labelElement.SetAttributeValue("pointer-events", "none");
                            if (label.Length * 6 > barWidth - 4)
                            {
                                labelElement.SetAttributeValue("textLength", Number(barWidth - 4));
                                labelElement.SetAttributeValue("lengthAdjust", "spacingAndGlyphs");
                            }
                            svg.Add(labelElement);
                        }
                    }
                    if (column.ProjectedAdditions != null)
                    {
                        for (var segmentIndex = 0; segmentIndex < chart.Segments.Count; segmentIndex++)
                        {
                            var segment = chart.Segments[segmentIndex];
                            var addition = column.ProjectedAdditions[segmentIndex];
                            var height = (double)(addition / axisMax) * PLOT_HEIGHT;
                            y -= height;
                            svg.Add(new XElement(_svg + "rect", Attr("x", center - barWidth / 2), Attr("y", y),
                                Attr("width", barWidth), Attr("height", height), new XAttribute("fill", Lighten(segment.Color)), SegmentLink(column.SegmentKeys == null ? null : column.SegmentKeys[segmentIndex] + "-projection"),
                                new XElement(_svg + "title", $"{group.Label} {column.Label}, {segment.Label} — odhad přírůstku: {Format(addition, chart.Unit)}, "
                                    + $"odhad celého období: {Format(column.Values[segmentIndex] + addition, chart.Unit)}")));
                        }
                    }
                    svg.Add(Text(center, BASELINE + 22, column.Label));
                }
                svg.Add(Text(LEFT + (index + 0.5) * groupWidth, BASELINE + 50, group.Label));
            }

            var legend = new XElement("div", new XAttribute("class", "legend"));
            foreach (var segment in chart.Segments)
                legend.Add(new XElement("span", new XElement("span", new XAttribute("class", "swatch"),
                    new XAttribute("style", "background:" + segment.Color), ""), segment.Label));
            if (chart.Groups.SelectMany(g => g.Columns).Any(c => c.ProjectedAdditions != null))
            {
                foreach (var segment in chart.Segments)
                    legend.Add(new XElement("span", new XElement("span", new XAttribute("class", "swatch"),
                        new XAttribute("style", "background:" + Lighten(segment.Color)), ""), segment.Label + " — odhad přírůstku"));
            }
            figure.Add(legend, new XElement("div", new XAttribute("class", "chart-scroll"), svg));
            return figure;
        }

        private static void Validate(GroupedStackedChart chart)
        {
            if (chart.Segments.Count == 0 || chart.Segments.Any(s => s == null || !Regex.IsMatch(s.Color ?? "", @"\A#[0-9a-fA-F]{6}\z")))
                throw new ArgumentException("Chart segments require colors in #RRGGBB format.");
            if (chart.Groups.Any(g => g == null || g.Columns.Count == 0 || g.Columns.Any(c => c == null ||
                (c.SegmentKeys != null && c.SegmentKeys.Length != chart.Segments.Count) ||
                (c.SegmentLabels != null && c.SegmentLabels.Length != chart.Segments.Count) ||
                c.Values == null || c.Values.Length != chart.Segments.Count || c.Values.Any(v => v < 0) ||
                (c.ProjectedAdditions != null && (c.ProjectedAdditions.Length != chart.Segments.Count || c.ProjectedAdditions.Any(v => v < 0))))))
                throw new ArgumentException("Each column requires one non-negative value per segment.");
        }

        private static object SegmentLink(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            return new[]
            {
                new XAttribute("data-link-key", key),
                new XAttribute("data-highlight-targets", key),
                new XAttribute("tabindex", "0")
            };
        }

        private static string Lighten(string color)
        {
            var components = Enumerable.Range(0, 3).Select(index =>
                Convert.ToInt32(color.Substring(1 + index * 2, 2), 16));
            return "#" + string.Concat(components.Select(value =>
                ((int)Math.Round(value * 0.25 + 255 * 0.75)).ToString("X2")));
        }

        private static decimal NiceStep(decimal value)
        {
            if (value <= 0) return 1;
            var magnitude = (decimal)Math.Pow(10, Math.Floor(Math.Log10((double)value)));
            var normalized = value / magnitude;
            return (normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10) * magnitude;
        }

        private static string Format(decimal value, string unit)
            => value.ToString("N0", HtmlDocumentRenderer.Culture) + " " + unit;
        private static string Number(double value)
            => value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        private static XAttribute Attr(string name, double value) => new XAttribute(name, Number(value));
        private static XElement Text(double x, double y, string text, string anchor = "middle")
            => new XElement(_svg + "text", Attr("x", x), Attr("y", y), new XAttribute("text-anchor", anchor),
                new XAttribute("font-size", "12"), new XAttribute("fill", "#475569"), text);
    }
}
