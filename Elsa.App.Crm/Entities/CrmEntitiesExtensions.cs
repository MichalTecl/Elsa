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
    }
}
