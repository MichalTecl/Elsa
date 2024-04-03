using System.Text;

namespace Robowire.RobOrm.Core.Query.Filtering.QuerySegments
{
    public interface IBooleanSegment
    {
        void RenderAsBoolean(StringBuilder sb);
    }
}
