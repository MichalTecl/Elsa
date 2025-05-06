using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Model
{
    public class CrmRobotModel
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public bool IsActive { get; set; }
        public List<string> MailRecipients { get; set; } = new List<string>();
        public DistributorGridFilter Filter { get; set; } = new DistributorGridFilter();
        public string MatchSetsTagTypeName { get; set; }
        public string UnmatchSetsTagTypeName { get; set; }
        public string MatchRemovesTagTypeName { get; set; }
        public string UnmatchRemovesTagTypeName { get; set; }
        public int SequenceOrder { get; set; }
        public bool CanMoveUp { get; set; }
        public bool CanMoveDown { get; set; }
    }
}
