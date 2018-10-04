using System.Web;

namespace Elsa.Common
{
    public interface ICustomResult
    {
        void WriteResponse(HttpContextBase context);
    }
}
