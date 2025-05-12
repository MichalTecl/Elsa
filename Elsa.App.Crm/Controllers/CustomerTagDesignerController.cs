using Elsa.App.Crm.Model;
using Elsa.App.Crm.Repositories;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Common;
using Robowire.RoboApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Controllers
{
    [Controller("CustomerTagDesigner")]
    public class CustomerTagDesignerController : ElsaControllerBase
    {
        private readonly CustomerTagRepository _tagRepository;

        public CustomerTagDesignerController(IWebSession webSession, ILog log, CustomerTagRepository tagRepository) : base(webSession, log)
        {
            _tagRepository = tagRepository;
        }

        public List<CustomerTagTypeGroupModel> GetGroups()
        {
            return _tagRepository.GetGroups().Select(g => new CustomerTagTypeGroupModel { Id = g.Id, Name = g.Name }).ToList();
        }

        public List<CustomerTagTypeGroupModel> SaveGroup(int? id, string name)
        {
            _tagRepository.SaveGroup(id, name);

            return GetGroups();
        }

        public List<CustomerTagTypeModel> LoadTags(int groupId)
        {
            var groupData = _tagRepository.GetGroupData(groupId);

            var result = new List<CustomerTagTypeModel>(groupData.Tags.Count);

            foreach (var tag in groupData.Tags) 
            {
                var model = new CustomerTagTypeModel
                {
                    Id = tag.Id,
                    Name = tag.Name,
                    CssClass = tag.CssClass,
                    IsRoot = tag.IsRoot,
                };
                                
                result.Add(model);

                model.TransitonsFrom.AddRange(groupData.Transitions.Where(t => t.TargetTagTypeId == model.Id).Select(t => t.SourceTagTypeId));
                model.TransitionsTo.AddRange(groupData.Transitions.Where(t => t.SourceTagTypeId == model.Id).Select(t => t.TargetTagTypeId));

                if (model.TransitonsFrom.Count == 0)
                {
                    model.IsRoot = true;
                }

                if (model.IsRoot)
                    model.FirstRootId = model.Id;
            }


            void TraverseDown(CustomerTagTypeModel current, HashSet<int> visited)
            {                
                foreach(var child in current.TransitionsTo.Select(cid => result.Single(r => r.Id == cid)))
                {
                    if (!visited.Add(child.Id))
                    {
                        continue;
                    }

                    child.AllTransitionParents.Add(current.Id);

                    child.FirstRootId = current.FirstRootId;

                    TraverseDown(child, visited);
                }
            }

            // round 1 - spreading RootId downstream + collecting parents
            var visitedTags = new HashSet<int>();
            foreach(var tag in result)
                TraverseDown(tag, visitedTags);

            // round 2 - circles might endup without any root - need to pick one
        }
    }

    
}
