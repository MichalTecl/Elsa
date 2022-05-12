using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Crm.Model;
using Elsa.Commerce.Core.Model;

namespace Elsa.Commerce.Core.Crm
{
    public interface ICustomerRepository
    {
        void SyncCustomers(IEnumerable<IErpCustomerModel> source);

        void SyncShadowCustomers();

        void PutComment(int customerId, string body);

        CustomerOverview GetOverview(string email);

        IEnumerable<CustomerOverview> GetOverviews(IEnumerable<string> emails);

        void UpdateNewsletterSubscribersList(string sourceName, List<string> actualSubscriers);

        List<string> GetSubscribersToSync(string sourceName);
    }
}
