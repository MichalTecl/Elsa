using Elsa.Core.Entities.Commerce.Common;
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
        public static Contact ToRaynetContact(ICustomer customer, IEnumerable<string> customerGroups, IEnumerable<CompanyCategory> categories, IAddress deliveryAddress, Contact target = null) 
        {
            target = target ?? new Contact();

            target.Name = new[] { customer.Name, customer.Email, customer.CompanyName }.FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? $"NEMÁ_JMÉNO_ElsaId_{customer.Id}";
            target.Role = target.Role ?? "A_SUBSCRIBER";
            target.RegNumber = customer.CompanyRegistrationId;
            target.TaxNumber = customer.VatId;
            
            target.Category = new IdContainer { Id = FindCategory(customerGroups, categories) };            

            target.Addresses = new List<AddressBucket>();

            target.Addresses.Add(ToAddressBucket(string.IsNullOrWhiteSpace(customer.CompanyName) ? target.Name : customer.CompanyName, customer, customer.Phone, customer.Email));
                        
            if (deliveryAddress != null) 
            {
                target.Addresses.Add(ToAddressBucket("Poslední doručovací adresa", deliveryAddress, deliveryAddress.Phone, customer.MainUserEmail));
            }
                        
            return target;
        }

        private static AddressBucket ToAddressBucket(string name, IPostalAddress src, string phone, string email) 
        {
            var bucket = new AddressBucket
            {
                Address = new Address
                {
                    Name = name,
                    Street = src.GetFormattedStreetAndHouseNr(),
                    City = src.City,
                    ZipCode = src.Zip
                },
                ContactInfo = new ContactInfo {
                    Email = email,
                    Tel1 = phone
                }
            };

            return bucket;
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
