using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Noml.Core;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Common;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Elsa.Common.EntityComments.Impl
{
    public class EntityCommentRepository : IEntityCommentsRepository
    {
        private readonly IDatabase _db;
        private readonly ICache _cache;
        private readonly ISession _session;

        public EntityCommentRepository(IDatabase db, ICache cache, ISession session)
        {
            _db = db;
            _cache = cache;
            _session = session;
        }

        public Dictionary<int, List<EntityComment>> GetComments(string entityType, IEnumerable<int> ids)
        {            
            var all = _cache.ReadThrough(GetCacheKey(entityType), TimeSpan.FromMinutes(10), () =>
            {
                var recs = QueryComments(entityType);
                
                var result = new Dictionary<int, List<EntityComment>>();

                foreach(var src in recs)
                {
                    if(!result.TryGetValue(src.RecordId, out var set))
                        set = result[src.RecordId] = new List<EntityComment>();

                    set.Add(new EntityComment
                    {
                        Id = src.Id,
                        PostDt = src.WriteDt,
                        Author = src.Author,
                        RecordId = src.RecordId,
                        Text = src.Text,
                    });
                }

                return result;
            });

            var filter = new HashSet<int>(ids?.Where(id => id > 0) ??  Enumerable.Empty<int>());
            if (filter.Count == 0)
                return all;
            
            var filtered = new Dictionary<int, List<EntityComment>>(filter.Count);

            foreach (var include in filter)
                if (all.TryGetValue(include, out var f))
                    filtered[include] = f;
            
            return filtered;
        }

        public List<EntityComment> SaveComment(string entityType, int recordId, int? previousCommentId, string text)
        {
            using (var tx = _db.OpenTransaction()) 
            {
                if (previousCommentId != null)
                {
                    var oldRec = QueryComments(entityType, q => q.Where(r => r.Id == previousCommentId)).SingleOrDefault().Ensure("Trying to replace not existing entity");
                    
                    oldRec.DeleteDt = DateTime.UtcNow;
                    _db.Save(oldRec);
                }

                if (!string.IsNullOrEmpty(text))
                {
                    var record = _db.New<IEntityComment>();
                    record.RecordId = recordId;
                    record.ProjectId = _session.Project.Id;
                    record.AuthorId = _session.User.Id;
                    record.EntityType = entityType;
                    record.WriteDt = DateTime.Now;
                    record.ReplacedCommentId = previousCommentId;
                    record.Text = text;

                    _db.Save(record);
                }

                tx.Commit();
            }

            _cache.Remove(GetCacheKey(entityType));

            var allComments = GetComments(entityType, new int[] { recordId });
            if (allComments.TryGetValue(recordId, out var current))
                return current;

            return new List<EntityComment>(0);
        }

        private string GetCacheKey(string entityType) => $"entityComments_{_session.Project.Id}_{entityType}";

        private List<IEntityComment> QueryComments(string entityType, Func<IQueryBuilder<IEntityComment>, IQueryBuilder<IEntityComment>> modify = null)
        {
            var query = _db.SelectFrom<IEntityComment>()
                .Join(e => e.Author)
                .Where(e => e.ProjectId == _session.Project.Id)
                .Where(e => e.EntityType == entityType)
                .Where(e => e.DeleteDt == null)
                .OrderByDesc(e => e.WriteDt);

            query = modify?.Invoke(query) ?? query;

            var recs = query.Execute().ToList();

            var replacedComments = new HashSet<int>(recs.Where(r => r.ReplacedCommentId != null).Select(r => r.ReplacedCommentId.Value));

            return recs.Where(r => !replacedComments.Contains(r.Id)).ToList();
        }
    }
}
