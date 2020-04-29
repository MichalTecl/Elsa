using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Smtp.Core.Database
{
    public interface IRecipientListsRepository
    {
        IEnumerable<string> GetRecipients(string groupName);

        void SetRecipeints(string groupName, IEnumerable<string> recipients);

        IEnumerable<string> GetAllGroupNames();
    }
}
