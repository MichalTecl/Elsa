using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Model;

namespace Elsa.Integration.Erp.Flox.Protocol.CustomerModel
{
    internal class ErpPersonModel : IErpCustomerModel
    {
        public ErpPersonModel(PersonModel src)
        {
            ErpCustomerId = src.UserId;
            Email = src.Email?.Trim();
            Phone = NormalizePhoneNumber(src.Phone);
            Name = src.FirstName?.Trim();
            Surname = src.LastName?.Trim();
            IsActive = src.Active != 0;
            IsNewsletterSubscriber = src.Newsletter != 0;
            IsDistributor = src.Groups?.ToLowerInvariant().Contains("velkoo") ?? false;
        }
        
        public string ErpCustomerId { get; }

        public string Email { get; }

        public string Phone { get; }

        public string Name { get; }

        public string Surname { get; }

        public bool IsActive { get; }

        public bool IsNewsletterSubscriber { get; }

        public bool IsDistributor { get; }

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
