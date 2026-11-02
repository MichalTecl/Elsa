using System.Text;

namespace RobOrm.Core.Query.Filtering.QuerySegments
{
    public interface IBooleanSegment
    {
        void RenderAsBoolean(StringBuilder sb);
    }
}
