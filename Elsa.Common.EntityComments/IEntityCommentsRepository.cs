using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Common.EntityComments
{
    public interface IEntityCommentsRepository
    {
        Dictionary<int, List<EntityComment>> GetComments(string entityType, IEnumerable<int> ids);

        List<EntityComment> SaveComment(string entityType, int recordId, int? previousCommentId, string text);            
    }
}
