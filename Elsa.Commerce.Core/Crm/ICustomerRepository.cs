using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Crm.Model;
using Elsa.Commerce.Core.Model;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Crm;
using Elsa.Core.Entitites.Crm;

namespace Elsa.Commerce.Core.Crm
{
    public interface ICustomerRepository
    {
        ICustomer GetCustomer(int id);

        void SyncCustomers(IEnumerable<IErpCustomerModel> source);

        void SyncShadowCustomers();
                
        CustomerOverview GetOverview(string email);

        IEnumerable<CustomerOverview> GetOverviews(IEnumerable<string> emails);
                
        List<string> GetSubscribersToSync(string sourceName);

        Dictionary<string, ICustomerGroupType> GetCustomerGroupTypes();

        Dictionary<int, IAddress> GetDistributorDeliveryAddressesIndex();

        Dictionary<int, string> GetCustomerSalesRepresentativeEmailIndex();
               
        void SnoozeCustomer(int customerId);    
        
        ICustomerChangeLog LogCustomerChange(int customerId, string field, object oldValue, object newValue, string groupingKey = null);

        List<ICustomerRelatedNote> GetCustomerRelatedNotes(int customerId);

        void AddCustomerNote(int customerId, string text);
        void DeleteCustomerNote(int noteId);

        Dictionary<int, string> GetDistributorNameIndex();
    }
}
