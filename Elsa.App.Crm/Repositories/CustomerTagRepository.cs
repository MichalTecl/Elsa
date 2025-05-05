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

        public CustomerTagRepository(IDatabase database, ISession session, ICache cache, ILog log)
        {
            _database = database;
            _session = session;
            _cache = cache;

            _tagTypeRepo = new AutoRepo<ICustomerTagType>(session, database, cache);
            _log = log;
        }

        public List<ICustomerTagType> GetTagTypes(bool assignableOnly, bool allAuthors = false)
        {
            return _tagTypeRepo.GetAll()
                .Where(t => ((!assignableOnly) || t.CanBeAssignedManually)
                && ((!t.ForAuthorOnly) || allAuthors || (t.AuthorId == _session.User.Id))).ToList();
        }

        public ICustomerTagType CreateTagType(string name, string description, int priority, bool isPrivate, string cssClass)
        {
            var existing = GetTagTypes(false, !isPrivate).FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (existing != null) 
            {
                throw new ArgumentException($"Štítek \"{name}\" již existuje");
            }

            return _tagTypeRepo.Create(t => {
                t.Name = name;
                t.Description = description;
                t.Priority = priority;
                t.CanBeAssignedManually = true;
                t.ForAuthorOnly = isPrivate;
                t.CssClass = cssClass;
            });
        }

        public ICustomerTagType UpdateTagType(int typeId, string name, string description, int priority, bool isPrivate, string cssClass)
        {
            var concurrent = GetTagTypes(false, false).FirstOrDefault(t => 
               t.Id != typeId  // another record
            && t.Name.Equals(name, StringComparison.OrdinalIgnoreCase) // same name
            && ((!isPrivate) // we are making it public
                || (!t.ForAuthorOnly) // concurrent one is public
                || (t.AuthorId == _session.User.Id) // or is of the same user
                ));

            if (concurrent != null)
                throw new ArgumentException($"Štítek \"{name}\" již existuje");

            return _tagTypeRepo.UpdateSingle(typeId, t => 
            {
                if ((!t.ForAuthorOnly) && (isPrivate))
                {
                    throw new ArgumentException("Štítek viditelný všem uživatelům nelze změnit na soukromý");
                }

                t.Name = name;
                t.Description = description;
                t.Priority = priority;
                t.CanBeAssignedManually = true;
                t.ForAuthorOnly = isPrivate;
                t.CssClass = cssClass;
            });            
        }        

        public List<ICustomerTagType> GetCustomerTags(int customerId, bool acrossUsers = false)
        {
            if(!GetAllTagAssignments().TryGetValue(customerId, out var assignments))
                return new List<ICustomerTagType>(0);

            return GetTagTypes(true, acrossUsers).Where(tt => assignments.Contains(tt.Id)).ToList();
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

            using (var tx = _database.OpenTransaction())
            {
                var tagType = GetTagTypes(true).FirstOrDefault(tt => tt.Id == tagTypeId) ?? throw new ArgumentException($"Cannot assign tag type id {tagTypeId}");

                var existingAssignments = GetAllTagAssignments();

                foreach (var customerId in customerIds)
                {                    
                    if (existingAssignments.TryGetValue(customerId, out var alreadyHasTags) && alreadyHasTags.Contains(tagTypeId))
                        continue;

                    var rec = _database.New<ICustomerTagAssignment>();
                    rec.CustomerId = customerId;
                    rec.TagTypeId = tagTypeId;
                    rec.AssignDt = DateTime.Now;
                    rec.AuthorId = _session.User.Id;

                    _database.Save(rec);

                    result.Add(customerId);
                }

                tx.Commit();
            }
            
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
                var assignments = _database.SelectFrom<ICustomerTagAssignment>().Where(a => a.TagTypeId == id).Execute().ToList();

                _database.DeleteAll(assignments);

                _log.Info($"Deleted {assignments.Count} of assignments");

                var tagType = _database.SelectFrom<ICustomerTagType>().Where(t => t.Id == id).Execute().FirstOrDefault().Ensure();

                _log.Info($"Deleting tag {tagType.Name}");

                _database.Delete(tagType);

                tx.Commit();
            }
        }

        internal int GetAssignmentsCount(int id)
        {
            return _database.SelectFrom<ICustomerTagAssignment>().Where(a => a.TagTypeId == id).Execute().Count();
        }
    }
}
