using Elsa.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core
{
    public static class CustomerUidCalculator
    {
        public static string GetCustomerUid(string companyId, string personId, string email)
        {
            if (!string.IsNullOrWhiteSpace(personId))
                return $"P{personId.Trim()}";

            if (!string.IsNullOrWhiteSpace(companyId))
                return $"C{companyId.Trim()}";

            if(!string.IsNullOrWhiteSpace(email))
                return $"X{email.ToLowerInvariant().Trim()}";

            return "???UID_N/A";
        }
    }
}
