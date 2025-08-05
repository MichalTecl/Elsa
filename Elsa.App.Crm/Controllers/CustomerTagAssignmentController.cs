using Elsa.App.Crm.Model;
using Elsa.App.Crm.Repositories;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Robowire.RoboApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Controllers
{
    [Controller("CustomerTagAssignment")]
    public class CustomerTagAssignmentController : ElsaControllerBase
    {
        private readonly CustomerTagRepository _customerTagRepository;
        public CustomerTagAssignmentController(IWebSession webSession, ILog log, CustomerTagRepository customerTagRepository) : base(webSession, log)
        {
            _customerTagRepository = customerTagRepository;
        }

        public IReadOnlyCollection<CustomerTagAssignmentInfo> Unassign(int customerId, int tagTypeId)
        {
            _customerTagRepository.Unassign(new[] { customerId }, tagTypeId);
            return _customerTagRepository.GetAssignments(new[] { customerId });
        }

        public IReadOnlyCollection<CustomerTagAssignmentInfo> Assign(int customerId, int tagTypeId, string note) 
        {
            _customerTagRepository.Assign(new[] { customerId }, tagTypeId, note);
            return _customerTagRepository.GetAssignments(new[] { customerId });
        }    
        
        public List<TagTransitionInfo> GetTransitions(int customerId, int tagTypeId)
        {
            var transitions = _customerTagRepository.GetPossibleTransitions(customerId, tagTypeId);

            return transitions.Select(t => new TagTransitionInfo
            {
                CustomerId = customerId,
                TagTypeId = t.Id,
                TagTypeCssClass = t.CssClass,
                TagTypeName = t.Name,
                FromTagTypeId = tagTypeId,
                DaysToWarning = t.DaysToWarning ?? 0,
                TagTypeGroupId = t.GroupId,
                RequiresNote = t.RequiresNote == true
            })
                .ToList();
        }

        public TagTransitionInfo CreateRootLevelTransiton(int customerId, string tagTypeName)
        {
            // TODO - tady by bylo celkem spravne kontrolovat, jestli je transition na cilovy tag pro zakaznika valdini, ale to je tbd...

            var tag = _customerTagRepository.GetTagTypes(null).FirstOrDefault(t => t.Name == tagTypeName) ?? throw new ArgumentException("Neznamy nazev tagu");

            var transition = new TagTransitionInfo
            {
                TagTypeId = tag.Id,
                CustomerId = customerId,
                TagTypeName = tagTypeName,
                TagTypeCssClass = tag.CssClass,
                RequiresNote = tag.RequiresNote == true,
                TagTypeGroupId = tag.GroupId
            };

            return transition;
        }
    }
}
