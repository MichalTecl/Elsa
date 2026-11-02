using System.Text;

namespace RobOrm.Core.Query.Filtering
{
    public interface IQuerySegment
    {
        void Render(StringBuilder sb);
    }
}
