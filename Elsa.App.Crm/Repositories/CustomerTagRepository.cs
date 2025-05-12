using Elsa.App.Crm.Entities;
using Elsa.Common.Caching;
using Elsa.Common.Data;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Repositories
{
    public class CustomerTagRepository 
    {
        private readonly IDatabase _database;
        private readonly ISession _session;
        private readonly ICache _cache;
        private readonly ILog _log;

        private readonly AutoRepo<ICustomerTagType> _tagTypeRepo;
        private readonly AutoRepo<ICustomerTagTypeGroup> _tagTypeGroupRepo;
        private readonly AutoRepo<ICustomerTagTransition> _transitionRepo;

        public CustomerTagRepository(IDatabase database, ISession session, ICache cache, ILog log)
        {
            _database = database;
            _session = session;
            _cache = cache;
            _log = log;

            _tagTypeRepo = new AutoRepo<ICustomerTagType>(session, database, cache, (db, q) => q.OrderBy(r => r.Name));
            _tagTypeGroupRepo = new AutoRepo<ICustomerTagTypeGroup>(session, database, cache);
            _transitionRepo = new AutoRepo<ICustomerTagTransition>(session, database, cache);
        }

        private void ClearMetadataCache()
        {
            _cache.Remove("customerTagGroups");
        }

        public TagGroup GetGroupData(int groupId)
        {
            return GetData().FirstOrDefault(g => g.Group.Id == groupId);
        }

        public List<TagGroup> GetData()
        {
            return _cache.ReadThrough("customerTagGroups", TimeSpan.FromHours(1), () => {

                var allGroups = _tagTypeGroupRepo.GetAll();
                var allTags = _database.SelectFrom<ICustomerTagType>().Execute().ToList();
                var allTransitions = _database.SelectFrom<ICustomerTagTransition>().Execute().ToList();

                var result = new List<TagGroup>(allGroups.Count);

                foreach(var grp in allGroups)
                {
                    var groupModel = new TagGroup(grp);
                    result.Add(groupModel);

                    ICustomerTagType tag = null;
                    while((tag = allTags.FirstOrDefault(t => t.GroupId == grp.Id)) != null)
                    {
                        allTags.Remove(tag);
                        groupModel.Tags.Add(tag);

                        ICustomerTagTransition transition = null;

                        while((transition = allTransitions.FirstOrDefault(t => t.SourceTagTypeId == tag.Id)) != null)
                        {
                            allTransitions.Remove(transition);
                            groupModel.Transitions.Add(transition);
                        }
                    }
                }

                return result;            
            });
        }

        public List<ICustomerTagTypeGroup> GetGroups()
        {
            return GetData().Select(d => d.Group).ToList();
        }

        public List<ICustomerTagTypeGroup> SaveGroup(int? id, string name)
        {
            _tagTypeGroupRepo.Upsert(id, r => r.Name = name);

            ClearMetadataCache();

            return GetGroups();
        }
               
        
        public List<ICustomerTagType> GetTagTypes(int? groupId)
        {
            return GetData().Where(g => groupId == null || g.Group.Id == groupId).SelectMany(g => g.Tags).ToList();
        }
              
        public List<ICustomerTagType> GetCustomerTags(int customerId, bool acrossUsers = false)
        {
            if(!GetAllTagAssignments().TryGetValue(customerId, out var assignments))
                return new List<ICustomerTagType>(0);

            return GetTagTypes(null).Where(tt => assignments.Contains(tt.Id)).ToList();
        }

        /// <summary>
        /// returns list of Ids where new assignment was created
        /// </summary>
        /// <param name="customerIds"></param>
        /// <param name="tagTypeId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public List<int> Assign(int[] customerIds, int tagTypeId)
        {
            var result = new List<int>(customerIds.Length);

            _database.Sql().Call("AssignTagToCustomers")
                .WithParam("@authorId", _session.User.Id)
                .WithParam("@tagTypeId", tagTypeId)
                .WithStructuredParam("@customerIds", "IntTable", customerIds, new[] { "Id" }, i => new object[] { i })
                .ReadRows(r => result.Add(r.GetInt32(0)));

            _cache.Remove(AssignmentsCacheKey);

            return result;
        }
                
        /// <summary>
        /// returns list of customerIds where existing assignment was removed
        /// </summary>
        /// <param name="customerIds"></param>
        /// <param name="tagTypeId"></param>
        /// <returns></returns>
        public List<int> Unassign(int[] customerIds, int tagTypeId)
        {
            var result = new List<int>(customerIds.Length);

            var assignments = GetAllTagAssignments();

            foreach (var customerId in customerIds) 
            {
                if (!assignments.TryGetValue(customerId, out var hasTags) || !hasTags.Contains(tagTypeId))
                    continue;

                var assignment = _database.SelectFrom<ICustomerTagAssignment>().Where(t => t.CustomerId == customerId && t.TagTypeId == tagTypeId).Take(1).Execute().Single();
                _database.Delete(assignment);
                
                result.Add(assignment.CustomerId);
            }
            
            _cache.Remove(AssignmentsCacheKey);

            return result;
        }

        private string AssignmentsCacheKey => $"customerTagAssignments_{_session.Project.Id}";

        private Dictionary<int, List<int>> GetAllTagAssignments()
        {
            return _cache.ReadThrough(AssignmentsCacheKey, TimeSpan.FromMinutes(10),
                () =>
                {
                    var all = _database.SelectFrom<ICustomerTagAssignment>().Join(a => a.Customer).Where(a => a.Customer.ProjectId == _session.Project.Id).Execute();

                    var result = new Dictionary<int, List<int>>();

                    foreach(var a in all)
                    {
                        if(!result.TryGetValue(a.CustomerId, out List<int> list))
                        {
                            list = new List<int>();
                            result[a.CustomerId] = list;
                        }

                        list.Add(a.TagTypeId);
                    }

                    return result;
                });
        }

        internal void DeleteTagType(int id)
        {
            _log.Info($"Deletion of tag Id={id} requested");

            using (var tx = _database.OpenTransaction())
            {
                var transitions = _database.SelectFrom<ICustomerTagTransition>().Where(t => t.SourceTagTypeId == id || t.TargetTagTypeId == id).Execute().ToList();
                _database.DeleteAll(transitions);

                var assignments = _database.SelectFrom<ICustomerTagAssignment>().Where(a => a.TagTypeId == id).Execute().ToList();

                _database.DeleteAll(assignments);

                _log.Info($"Deleted {assignments.Count} of assignments");

                var tagType = _database.SelectFrom<ICustomerTagType>().Where(t => t.Id == id).Execute().FirstOrDefault().Ensure();

                _log.Info($"Deleting tag {tagType.Name}");

                _database.Delete(tagType);

                tx.Commit();
            }

            ClearMetadataCache();
        }

        internal int GetAssignmentsCount(int id)
        {
            return _database.SelectFrom<ICustomerTagAssignment>().Where(a => a.TagTypeId == id).Execute().Count();
        }

        public class TagGroup
        {
            public ICustomerTagTypeGroup Group { get; }

            public TagGroup(ICustomerTagTypeGroup group)
            {
                Group = group;
            }

            public List<ICustomerTagType> Tags { get; } = new List<ICustomerTagType>();
            public List<ICustomerTagTransition> Transitions { get; } = new List<ICustomerTagTransition>();
        }
    }
}
