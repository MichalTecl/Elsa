using Elsa.Common.Interfaces;
using System;
using System.Linq;

namespace Elsa.Common.EntityComments.Impl
{
    public class EntityCommentsFacade : IEntityCommentsFacade
    {
        private readonly IEntityCommentsRepository _repository;
        private readonly IUserNickProvider _nickProvider;
        private readonly ISession _session;

        public EntityCommentsFacade(IEntityCommentsRepository repository, IUserNickProvider nickProvider, ISession session)
        {
            _repository = repository;
            _nickProvider = nickProvider;
            _session = session;
        }

        public void AddComment(UserRight commentWriteUserRight, ISingleCommentEntity data)
        {
            _session.EnsureUserRight(commentWriteUserRight);

            var prevComment = LoadCurrentComment(data);

            int? prevCommentId = prevComment?.Id;

            if ((prevComment != null && prevComment.Text == data.CommentText) || (prevComment == null && string.IsNullOrEmpty(data.CommentText)))
                return;

            _repository.SaveComment(data.EntityTypeName, data.RecordId, prevCommentId, data.CommentText);
        }

        public void TryLoadComment(UserRight commentReadUserRight, ISingleCommentEntity entity)
        {
            EntityComment comment = null;

            if (_session.HasUserRight(commentReadUserRight))
            {
                comment = LoadCurrentComment(entity);
            }

            entity.CommentDt = comment?.PostDt;
            entity.CommentText = comment?.Text;
            entity.CommentAuthorNick = comment == null ? null : _nickProvider.GetUserNick(comment.Author.Id);
        }

        private EntityComment LoadCurrentComment(ISingleCommentEntity data)
        {
            var existing = _repository.GetComments(data.EntityTypeName, new int[] { data.RecordId });
            if (existing.TryGetValue(data.RecordId, out var invalidComments))
                return invalidComments.OrderByDescending(c => c.Id).FirstOrDefault();

            return null;
        }
               
    }
}
