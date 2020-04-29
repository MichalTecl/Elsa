using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Smtp.Core.Database;
using Robowire.RoboApi;

namespace Elsa.Smtp.Core.Controllers
{
    [Controller("recipientListsSettings")]
    public class RecipientListsSettingsController : ElsaControllerBase
    {
        private readonly IRecipientListsRepository m_repository;

        public RecipientListsSettingsController(IWebSession webSession, ILog log, IRecipientListsRepository repository) : base(webSession, log)
        {
            m_repository = repository;
        }

        public IEnumerable<RecipientListModel> GetLists()
        {
            foreach (var groupName in m_repository.GetAllGroupNames())
            {
                yield return new RecipientListModel
                {
                    GroupName = groupName,
                    Addresses = string.Join("; ", m_repository.GetRecipients(groupName))
                };
            }
        }

        public IEnumerable<RecipientListModel> UpdateGroup(RecipientListModel list)
        {
            if (string.IsNullOrWhiteSpace(list.GroupName))
            {
                throw new InvalidOperationException("Group name cannot be empty");
            }

            var recipients = (list.Addresses ?? string.Empty).Split(';', ',', '\r', '\n')
                .Select(a => a.Trim().ToLowerInvariant()).Where(a => !string.IsNullOrWhiteSpace(a));

            m_repository.SetRecipeints(list.GroupName, recipients);

            return GetLists();
        }
    }
}
