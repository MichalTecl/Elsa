using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Crm;
using Elsa.Core.Entities.Commerce.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Jobs.ExternalSystemsDataPush.Mappers
{    
    internal static class CustomerMapper
    {
        public const string PRIMARY_ADDRESS_NAME = "Sídlo klienta";
        public const string DELIVERY_ADDRESS_NAME = "Poslední doručovací adresa";

        public static ContactDetail ToRaynetContact(ICustomer customer, IEnumerable<string> customerGroups, IEnumerable<CompanyCategory> categories, IAddress deliveryAddress, long? contactSourceId, ContactDetail target = null) 
        {
            target = target ?? new ContactDetail();

            target.Name = new[] { customer.Name, customer.Email, customer.CompanyName }.FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? $"NEMÁ_JMÉNO_ElsaId_{customer.Id}";
            target.Role = target.Role ?? "A_SUBSCRIBER";
            target.RegNumber = customer.CompanyRegistrationId;
            target.TaxNumber = customer.VatId;
                                                
            if (contactSourceId != null) 
            {
                target.ContactSource = new IdContainer { Id = contactSourceId };
            }

            target.Category = new IdContainer { Id = FindCategory(customerGroups, categories) };            

            target.Addresses = new List<AddressBucket>();

            target.Addresses.Add(ToAddressBucket(PRIMARY_ADDRESS_NAME, customer, customer.Phone, customer.Email, isPrimary: true));
                        
            if (deliveryAddress != null) 
            {
                target.Addresses.Add(ToAddressBucket(DELIVERY_ADDRESS_NAME, deliveryAddress, deliveryAddress.Phone, customer.MainUserEmail, isPrimary: false));
            }
                        
            return target;
        }

        private static AddressBucket ToAddressBucket(string name, IPostalAddress src, string phone, string email, bool isPrimary) 
        {
            var bucket = new AddressBucket
            {
                Primary = isPrimary,
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
