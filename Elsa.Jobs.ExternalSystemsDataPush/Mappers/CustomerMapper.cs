using Elsa.Core.Entities.Commerce.Crm;
using Elsa.Core.Entities.Commerce.Extensions;
using Elsa.Integration.Crm.Raynet.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Jobs.ExternalSystemsDataPush.Mappers
{
    internal static class CustomerMapper
    {
        public static Contact ToRaynetContact(ICustomer customer, IEnumerable<string> customerGroups, IEnumerable<CompanyCategory> categories, Contact target = null) 
        {
            target = target ?? new Contact();

            target.Name = new[] { customer.Name, customer.Email, customer.CompanyName }.FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? $"NEMÁ_JMÉNO_ElsaId_{customer.Id}";
            target.Role = target.Role ?? "A_SUBSCRIBER";
            target.RegNumber = customer.CompanyRegistrationId;
            
            target.Category = new IdContainer { Id = FindCategory(customerGroups, categories) };            

            target.Addresses = target.Addresses ?? new List<AddressBucket>();

            var addressBucket = target.Addresses.FirstOrDefault();
            if (addressBucket == null)
            {
                addressBucket = new AddressBucket();
                target.Addresses.Add(addressBucket);
            }

            addressBucket.Address = addressBucket.Address ?? new Address();

            var address = addressBucket.Address;
            
            address.Name = string.IsNullOrWhiteSpace(customer.CompanyName) ? target.Name : customer.CompanyName;
            address.Street = customer.GetFormattedStreetAndHouseNr();
            address.City = customer.City;
            address.Country = customer.Country;
            address.ZipCode = customer.Zip;

            addressBucket.ContactInfo = addressBucket.ContactInfo ?? new ContactInfo();
            addressBucket.ContactInfo.Email = customer.Email;
            addressBucket.ContactInfo.Tel1 = customer.Phone;

            return target;
        }

        private static long? FindCategory(IEnumerable<string> customerGroups, IEnumerable<CompanyCategory> categories) 
        {
            foreach(var group in customerGroups) 
            {
                var cat = categories.FirstOrDefault(c => c.Code01 != null && c.Code01.Equals(group, StringComparison.InvariantCultureIgnoreCase));
                if (cat != null)
                    return cat.Id;
            }

            return null;
        }
    }
}
