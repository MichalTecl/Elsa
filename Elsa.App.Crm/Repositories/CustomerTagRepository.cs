using Elsa.App.Crm.Entities;
using Elsa.App.Crm.Model;
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
                
        public TagGroup GetGroupData(int groupId)
        {
            return GetData().FirstOrDefault(g => g.Group.Id == groupId);
        }

        public string BindMetadataCacheKey(string key)
        {
            _transitionRepo.BindCacheKey(key);
            _tagTypeRepo.BindCacheKey(key);
            _tagTypeGroupRepo.BindCacheKey(key);

            return key;
        }

        public List<TagGroup> GetData()
        {
            return _cache.ReadThrough(BindMetadataCacheKey("customerTagGroups"), TimeSpan.FromHours(1), () => {

                var allGroups = _tagTypeGroupRepo.GetAll();
                var allTags = new List<ICustomerTagType>(_tagTypeRepo.GetAll());
                var allTransitions = new List<ICustomerTagTransition>(_transitionRepo.GetAll());

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

        public Dictionary<int, string> GetGroupsSearchTags()
        {
            return _cache.ReadThrough(BindMetadataCacheKey("groupsSearchTagIndex"), TimeSpan.FromHours(1), () =>
            {
                var groups = _tagTypeGroupRepo.GetAll();

                var result = new Dictionary<int, string>();

                int groupId = -1;
                var sb = new StringBuilder();

                void CloseGroup()
                {
                    if (groupId < 1 || sb.Length == 0)
                        return;

                    result.Add(groupId, sb.ToString());
                    sb.Clear();
                }

                foreach (var tag in _tagTypeRepo.GetAll().OrderBy(t => t.GroupId))
                {
                    if (tag.GroupId != groupId)
                    {
                        CloseGroup();

                        groupId = tag.GroupId;
                        sb.Append(groups.Single(g => g.Id == groupId).Name);
                    }

                    sb.Append("|").Append(tag.Name);
                }

                CloseGroup();

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

            return GetGroups();
        }
               
        
        public List<ICustomerTagType> GetTagTypes(int? groupId)
        {
            return GetData().Where(g => groupId == null || g.Group.Id == groupId).SelectMany(g => g.Tags).ToList();
        }
              
        public List<ICustomerTagType> GetCustomerTags(int customerId)
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
        public List<int> Assign(int[] customerIds, int tagTypeId, string note)
        {
            var result = new List<int>(customerIds.Length);

            _database.Sql().Call("AssignTagToCustomers")
                .WithParam("@authorId", _session.User.Id)
                .WithParam("@tagTypeId", tagTypeId)
                .WithParam("@note", string.IsNullOrWhiteSpace(note) ? null : note)
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

                var assignment = _database.SelectFrom<ICustomerTagAssignment>()
                    .Where(t => t.CustomerId == customerId 
                                && t.TagTypeId == tagTypeId 
                                && t.UnassignDt == null).Take(1).Execute().Single();

                if (assignment.Note != null)
                {
                    assignment.UnassignDt = DateTime.Now;
                    _database.Save(assignment);
                }
                else
                {
                    _database.Delete(assignment);
                }

                result.Add(assignment.CustomerId);
            }
            
            _cache.Remove(AssignmentsCacheKey);

            return result;
        }

        public IReadOnlyCollection<CustomerTagAssignmentInfo> GetAssignments(IEnumerable<int> customerIds)
        {
            return _database.Sql()
                .Call("LoadTagAssignmentsInfo")
                .WithStructuredParam("@customerIds", "IntTable", customerIds, new[] { "Id" }, i => new object[] { i })
                .AutoMap<CustomerTagAssignmentInfo>()
                .AsReadOnly();
        }

        private string AssignmentsCacheKey => $"customerTagAssignments_{_session.Project.Id}";

        private Dictionary<int, List<int>> GetAllTagAssignments()
        {
            return _cache.ReadThrough(AssignmentsCacheKey, TimeSpan.FromMinutes(10),
                () =>
                {
                    var all = _database.SelectFrom<ICustomerTagAssignment>().Join(a => a.Customer)
                    .Where(a => a.Customer.ProjectId == _session.Project.Id
                             && a.UnassignDt == null).Execute();

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
                var robots = _database.SelectFrom<ICrmRobot>().Where(r => r.FilterMatchSetsTagTypeId == id || r.FilterUnmatchSetsTagTypeId == id || r.FilterMatchRemovesTagTypeId == id || r.FilterUnmatchRemovesTagTypeId == id).Execute().ToList();
                if (robots.Count > 0)
                {
                    throw new ArgumentException($"Nelze smazat štítek, prootže jej používají tito roboti: {string.Join(", ", robots.Select(r => r.Name))}");
                }

                _transitionRepo.DeleteWhere(t => t.SourceTagTypeId == id || t.TargetTagTypeId == id);

                var assignments = _database.SelectFrom<ICustomerTagAssignment>().Where(a => a.TagTypeId == id).Execute().ToList();

                if (assignments.Count > 0)
                {
                    _database.DeleteAll(assignments);
                    _cache.Remove(AssignmentsCacheKey);                    
                }

                _tagTypeRepo.DeleteWhere(t => t.Id == id);
                                
                tx.Commit();
            }

            
        }

        internal int GetAssignmentsCount(int id)
        {
            return _database.SelectFrom<ICustomerTagAssignment>().Where(a => a.TagTypeId == id && a.UnassignDt == null).Execute().Count();
        }

        public ICustomerTagType SaveTag(int groupId, int? id, Action<ICustomerTagType> setup)
        {
            return _tagTypeRepo.Upsert(id, t => { 
                
                if (t.AuthorId == 0)
                {
                    t.AuthorId = _session.User.Id;                    
                }

                t.ProjectId = _session.Project.Id;

                if (id == null)
                {
                    t.IsRoot = true;
                    t.GroupId = groupId;
                }

                var originalGroupId = t.GroupId;
                
                setup(t);

                if (t.GroupId != originalGroupId || t.GroupId != groupId) 
                {
                    throw new ArgumentException("Není možné přesouvat štítky mezi skupinami");
                }

                var existing = _tagTypeRepo.GetAll().FirstOrDefault(ex => ex.Name.Equals(t.Name, StringComparison.InvariantCultureIgnoreCase));
                if (existing != null && (id == null || id != existing.Id))
                    throw new ArgumentException($"Štítek s názvem '{t.Name}' již existuje");

                if (string.IsNullOrEmpty(t.Description))
                    t.Description = t.Name;

                if (string.IsNullOrEmpty(t.Name))
                    throw new ArgumentException("Štítek musí mít název");

                if (string.IsNullOrEmpty(t.CssClass))
                    throw new ArgumentException("Štítek musí mít třídu stylu");
                
            });
        }

        public void SetTagTransition(int sourceId, int targetId, bool throwIfExsits)
        {
            if (sourceId == targetId)
                throw new ArgumentException("Štítek nemůže přejít sám na sebe");

            using(var tx = _database.OpenTransaction())
            {
                var sourceTag = _tagTypeRepo.Get(sourceId);
                var targetTag = _tagTypeRepo.Get(targetId);

                if (sourceTag.GroupId != targetTag.GroupId)
                    throw new ArgumentException("Není možné propojovat štítky napříč skupinami");

                var existingTransition = _transitionRepo.GetAll().FirstOrDefault(ex => ex.SourceTagTypeId == sourceId && ex.TargetTagTypeId == targetId);
                if (existingTransition != null)
                {
                    if (!throwIfExsits)
                    {
                        tx.Commit();
                        return;
                    }

                    throw new ArgumentException($"Přechod {sourceTag.Name} -> {targetTag.Name} již existuje");
                }

                var transition = _database.New<ICustomerTagTransition>();
                transition.AuthorId = _session.User.Id;
                transition.SourceTagTypeId = sourceId;
                transition.TargetTagTypeId = targetId;
                _transitionRepo.Save(transition);

                if (targetTag.IsRoot)
                {
                    targetTag.IsRoot = false;
                    _tagTypeRepo.Save(targetTag);                
                }

                tx.Commit();
            }
        }

        public void RemoveTagTransition(int sourceId, int targetId)
        {
            using (var tx = _database.OpenTransaction())
            {
                var sourceTag = _tagTypeRepo.Get(sourceId);
                var targetTag = _tagTypeRepo.Get(targetId);
               
                var existingTransition = _transitionRepo.GetAll().FirstOrDefault(ex => ex.SourceTagTypeId == sourceId && ex.TargetTagTypeId == targetId);
                if (existingTransition == null)
                    return;

                var hasOtherParent = _transitionRepo.GetAll().Any(t => t.SourceTagTypeId != sourceId && t.TargetTagTypeId == targetId);
                
                if (!targetTag.IsRoot && !hasOtherParent)
                {
                    targetTag.IsRoot = true;
                    _tagTypeRepo.Save(targetTag);
                }

                _transitionRepo.DeleteWhere(t => t.Id == existingTransition.Id);

                tx.Commit();
            }
        }

        internal List<ICustomerTagType> GetPossibleTransitions(int customerId, int tagTypeId)
        {
            var result = new List<ICustomerTagType>();

            foreach(var group in GetData())
            {
                if(!group.Tags.Any(t => t.Id == tagTypeId))                
                    continue;

                var transitions = group.Transitions.Where(t => t.SourceTagTypeId == tagTypeId);

                foreach(var tran in transitions)
                {
                    result.Add(group.Tags.FirstOrDefault(t => t.Id == tran.TargetTagTypeId));
                }
            }

            return result;
        }

        internal void DeleteGroup(int groupId)
        {
            using (var tx = _database.OpenTransaction())
            {
                var tags = _database.SelectFrom<ICustomerTagType>()
                    .Where(t => t.GroupId == groupId).Execute().ToList();

                foreach (var tag in tags)
                    DeleteTagType(tag.Id);

                _tagTypeGroupRepo.DeleteWhere(g => g.Id == groupId);

                tx.Commit();
            }
        }

        internal GroupDeleteInfo GetGroupDeleteInfo(int groupId)
        {
            var group = GetGroupData(groupId);
            if (group == null)
                return new GroupDeleteInfo { Message = "Skupina nenalezena" };

            if (!group.Tags.Any())
                return new GroupDeleteInfo { CanDelete = true };

            var assignedTags = _database.SelectFrom<ICustomerTagAssignment>()
                                        .Join(ta => ta.TagType)
                                        .Where(a => a.TagType.GroupId == groupId)
                                        .Execute()
                                        .Select(ta => ta.TagType)
                                        .Distinct()
                                        .ToList();

            if (assignedTags.Any())
            {
                var tagNames = StringUtil.Limit(string.Join(", ", assignedTags.Select(t => t.Name).Distinct()), 100, "...");

                return new GroupDeleteInfo
                {
                    CanDelete = true,
                    NeedsConfirmation = true,
                    Message = $"POZOR - chystáte se smazat skupinu {group.Group.Name}, obsahující štítky přiřazené zákazníkům ({tagNames}). Opravdu chcete pokračovat?",
                };
            }

            return new GroupDeleteInfo
            {
                CanDelete = true,
                NeedsConfirmation = true,
                Message = $"Opravdu chcete smazat skupinu {group.Group.Name}, včetně {group.Tags.Count} štítků?"
            };

        }

        internal void UpdateAssignmentNote(int customerId, int tagTypeId, string note)
        {
            var assignment = _database.SelectFrom<ICustomerTagAssignment>()
                    .Where(t => t.CustomerId == customerId
                                && t.TagTypeId == tagTypeId
                                && t.UnassignDt == null).Take(1).Execute().FirstOrDefault() ?? throw new ArgumentException("Assignment does not exist");

            assignment.Note = note;
            _database.Save(assignment);

            _cache.Remove(AssignmentsCacheKey);
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
