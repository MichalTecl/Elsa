using System.Text;

namespace Robowire.RobOrm.Core.Query.Filtering
{
    public interface IQuerySegment
    {
        void Render(StringBuilder sb);
    }
}
