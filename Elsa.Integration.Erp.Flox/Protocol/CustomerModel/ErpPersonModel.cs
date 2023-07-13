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
