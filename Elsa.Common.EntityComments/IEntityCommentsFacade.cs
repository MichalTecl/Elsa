using Elsa.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Common.EntityComments
{
    public interface IEntityCommentsFacade
    {
        void AddComment(UserRight commentWirteUserRight, ISingleCommentEntity data);
        void TryLoadComment(UserRight commentReadUserRight, ISingleCommentEntity entity);
    }
}
