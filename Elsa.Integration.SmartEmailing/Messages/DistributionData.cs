using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Integration.SmartEmailing.Messages
{
    public class DistributionData
    {
        public int Total { get; set; }
        public int Confirmed { get; set; }
        public int Unsubscribed { get; set; }
    }
}
