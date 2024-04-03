using System.Text;

namespace Robowire.RobOrm.Core.Query.Filtering.QuerySegments
{
    public static class QuerySegmentExtensions
    {
        public static void RenderBoolean(this IQuerySegment segment, StringBuilder target)
        {
            var bRend = segment as IBooleanSegment;
            if (bRend != null)
            {
                bRend.RenderAsBoolean(target);
                return;
            }

            segment.Render(target);
        }
    }
}
