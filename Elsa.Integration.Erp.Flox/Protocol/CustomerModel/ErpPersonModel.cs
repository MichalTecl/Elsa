using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Model;
using Elsa.Common.Utils;

namespace Elsa.Integration.Erp.Flox.Protocol.CustomerModel
{
    internal class ErpPersonModel : IErpCustomerModel
    {
        public ErpPersonModel(PersonModel src)
        {
            ErpCustomerId = CustomerUidCalculator.GetCustomerUid(src.CompanyId, src.PersonId, src.Email);
            Email = src.Email?.Trim();
            Phone = NormalizePhoneNumber(src.Phone);
            Name = src.FirstName?.Trim();
            Surname = src.LastName?.Trim();
            IsActive = src.Active != 0;
            IsNewsletterSubscriber = src.Newsletter != 0;
            IsDistributor = (!string.IsNullOrEmpty(src.CompanyId)) || (src.Groups?.ToLowerInvariant().Contains("velkoo") ?? false);
            Groups = src.Groups;
            VatId = src.VatId;
            CompanyName = src.CompanyName;
            Street = src.Street;
            DescriptiveNumber = src.DescriptiveNumber;
            OrientationNumber = src.OrientationNumber;
            City = src.City;
            Zip = src.Zip;
            Country = src.Country;
        }
        
        public string ErpCustomerId { get; }

        public string Email { get; }

        public string Phone { get; }

        public string Name { get; }

        public string Surname { get; }

        public bool IsActive { get; }

        public bool IsNewsletterSubscriber { get; set; }

        public bool IsDistributor { get; }

        public string Groups { get; }

        public string VatId { get; set; }

        public string CompanyName { get; }

        public string Street { get; }

        public string DescriptiveNumber { get; }

        public string OrientationNumber { get; }

        public string City { get; }

        public string Zip { get; }

        public string Country { get; }

        private static string NormalizePhoneNumber(string srcPhone)
        {
            srcPhone = srcPhone?.Trim();
            if (string.IsNullOrWhiteSpace(srcPhone))
            {
                return null;
            }

            if (srcPhone.StartsWith("00"))
            {
                srcPhone = $"+{srcPhone.Substring(2)}";
            }

            if (!srcPhone.StartsWith("+"))
            {
                srcPhone = $"+420{srcPhone}";
            }

            return srcPhone;
        }

        
    }
}
