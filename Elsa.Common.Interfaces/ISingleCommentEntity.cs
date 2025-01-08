using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common.Interfaces
{
    public interface ISingleCommentEntity
    {
        int RecordId { get; }
        string EntityTypeName { get; }
        string CommentText { get; set; }
        DateTime? CommentDt { get; set; }
        string CommentAuthorNick { get; set; }
    }
}
