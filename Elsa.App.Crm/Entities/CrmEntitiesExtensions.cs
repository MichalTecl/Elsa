using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Crm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Entities
{
    public static class CrmEntitiesExtensions
    {
        public static IEnumerable<string> GetRecipients(this ICrmRobot robot, params string[] _allMailsPeek)
        {
            return string.IsNullOrWhiteSpace(robot.NotifyMailList?.Trim())
                ? _allMailsPeek
                : robot.NotifyMailList.Split(',', ';')
                .Select(m => m.Trim())
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Select(m => m.ToLowerInvariant())
                .Concat(_allMailsPeek)
                .Distinct();
        }

        public static EntityState GetState(this ICustomerStore store)
        {
            return new EntityState()
                .Add("Název", store.Name)
                .Add("Adresa", $"{store.Address} {store.City}")
                .Add("WWW", store.Www)
                .Add("GPS", $"{store.Lat}, {store.Lon}");
        }

        public static EntityState GetState(this IPerson person)
        {
            return new EntityState()
                .Add("Jméno", person.Name)
                .Add("Email", person.Email)
                .Add("Telefon", person.Phone)
                .Add("Adresa", person.Address)
                .Add("Poznámka", person.Note);
        }
    }
}
