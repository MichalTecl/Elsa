using Elsa.App.Crm.Model;
using Elsa.App.Crm.Repositories;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Robowire.RoboApi;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa.App.Crm.Controllers
{
    [Controller("CustomerTagDesigner")]
    public class CustomerTagDesignerController : ElsaControllerBase
    {
        private readonly CustomerTagRepository _tagRepository;
        private readonly ICache _cache;
        private readonly IDatabase _db;

        public CustomerTagDesignerController(IWebSession webSession, ILog log, CustomerTagRepository tagRepository, ICache cache, IDatabase db) : base(webSession, log)
        {
            _tagRepository = tagRepository;
            _cache = cache;
            _db = db;
        }

        public List<CustomerTagTypeGroupModel> GetGroups()
        {
            var srchIndex = _tagRepository.GetGroupsSearchTags();

            return _tagRepository.GetGroups().Select(g => {
                return new CustomerTagTypeGroupModel 
                { 
                    Id = g.Id, 
                    Name = g.Name, 
                    SearchTag = srchIndex.TryGetValue(g.Id, out var tag) ? tag : g.Name,
                };
            }).ToList();
        }

        public List<CustomerTagTypeGroupModel> SaveGroup(int? id, string name)
        {
            _tagRepository.SaveGroup(id, name);

            return GetGroups();
        }

        public List<CustomerTagTypeNodeModel> LoadTags(int groupId)
        {
            return _cache.ReadThrough(
                _tagRepository.BindMetadataCacheKey($"customerTags_prebuiltTree_{groupId}"), 
                TimeSpan.FromMinutes(10), 
                () =>
            {
                var groupData = _tagRepository.GetGroupData(groupId);

                var result = new List<CustomerTagTypeNodeModel>(groupData.Tags.Count);

                foreach (var tag in groupData.Tags)
                {
                    var model = new CustomerTagTypeNodeModel
                    {
                        Id = tag.Id,
                        Name = tag.Name,
                        CssClass = tag.CssClass,
                        IsRoot = tag.IsRoot,
                        DaysToWarning = tag.DaysToWarning ?? 0
                    };

                    result.Add(model);

                    model.TransitonsFrom.AddRange(groupData.Transitions.Where(t => t.TargetTagTypeId == model.Id).Select(t => t.SourceTagTypeId));
                    model.TransitionsTo.AddRange(groupData.Transitions.Where(t => t.SourceTagTypeId == model.Id).Select(t => t.TargetTagTypeId));

                    if (model.TransitonsFrom.Count == 0)
                    {
                        model.IsRoot = true;
                    }

                    if (model.IsRoot)
                        model.AnyRootId = model.Id;
                }

                void TraverseDown(CustomerTagTypeNodeModel current, HashSet<int> visited)
                {
                    foreach (var child in current.TransitionsTo.Select(cid => result.Single(r => r.Id == cid)))
                    {
                        if (!visited.Add(child.Id.Value))
                        {
                            continue;
                        }

                        child.AllTransitionParents.Add(current.Id.Value);

                        child.AnyRootId = current.AnyRootId;

                        TraverseDown(child, visited);
                    }
                }

                // round 1 - spreading RootId downstream + collecting parents
                var visitedTags = new HashSet<int>();
                foreach (var tag in result)
                    TraverseDown(tag, visitedTags);

                // round 2 - circles might end up without any root - need to elect one 
                var unrooted = result.Where(r => r.AnyRootId == null).ToList();

                foreach (var unr in unrooted)
                {
                    var newRootId = unr.AllTransitionParents.Count == 0 ? unr.Id : unr.AllTransitionParents.Min();

                    var newRoot = result.Single(r => r.Id == newRootId);
                    newRoot.IsRoot = true;
                    newRoot.AnyRootId = newRootId;

                    visitedTags.Clear();
                    TraverseDown(newRoot, visitedTags);
                }

                return result;
            });
        }

        public List<CustomerTagTypeNodeModel> DeleteTag(int groupId, int tagId)
        {
            _tagRepository.DeleteTagType(tagId);
            return LoadTags(groupId);
        }

        public List<CustomerTagTypeNodeModel> SaveTag(int groupId, int? parentTagId, CustomerTagTypeModel model)
        {
            using (var tx = _db.OpenTransaction())
            {
                var tag = _tagRepository.SaveTag(groupId, model.Id, (t) =>
                {
                    t.Name = model.Name;
                    t.CssClass = model.CssClass;
                    t.Description = model.Description;
                    t.DaysToWarning = model.DaysToWarning == 0 ? null : (int?)model.DaysToWarning;
                });

                if (parentTagId != null) 
                {
                    _tagRepository.SetTagTransition(parentTagId.Value, tag.Id, false);
                }

                tx.Commit();

                return LoadTags(groupId);
            }
        }

        public List<CustomerTagTypeNodeModel> SetTransition(int groupId, int sourceId, int targetId)
        {
            _tagRepository.SetTagTransition(sourceId, targetId, true);

            return LoadTags(groupId);
        }

        public List<CustomerTagTypeNodeModel> RemoveTransition(int groupId, int sourceId, int targetId)
        {
            _tagRepository.RemoveTagTransition(sourceId, targetId);

            return LoadTags(groupId);
        }
    }    
}
