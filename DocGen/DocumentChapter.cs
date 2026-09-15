using System;
using System.Collections.Generic;

namespace DocGen
{
    public interface IDocumentChapter
    {
        DocumentChapter Build(DateTime generatedAt);
    }

    public sealed class DocumentChapter
    {
        public string Title { get; set; }
        public string Text { get; set; }
        public GroupedStackedChart Chart { get; set; }
        public DocumentTable Table { get; set; }
    }

    public sealed class GroupedStackedChart
    {
        public string Title { get; set; }
        public string Unit { get; set; }
        public List<ChartSegment> Segments { get; } = new List<ChartSegment>();
        public List<ChartGroup> Groups { get; } = new List<ChartGroup>();
    }

    public sealed class ChartSegment
    {
        public string Label { get; set; }
        public string Color { get; set; }
    }

    public sealed class ChartGroup
    {
        public string Label { get; set; }
        public List<ChartColumn> Columns { get; } = new List<ChartColumn>();
    }

    public sealed class ChartColumn
    {
        public string Label { get; set; }
        // Values follow the segment order, from bottom to top.
        public decimal[] Values { get; set; }

        // Optional labels inside the actual segments, in the same order as Values.
        public string[] SegmentLabels { get; set; }

        // Stable identities shared with table cells; independent of display labels.
        public string[] SegmentKeys { get; set; }

        // Optional additional amounts, not projected totals; same order as Values.
        public decimal[] ProjectedAdditions { get; set; }
    }
}
