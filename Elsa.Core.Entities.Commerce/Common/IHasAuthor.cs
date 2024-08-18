using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common.Security;

namespace Elsa.Core.Entities.Commerce.Common
{
    public interface IHasAuthor
    {
        int AuthorId { get; set; }
        IUser Author { get; }
    }
}
