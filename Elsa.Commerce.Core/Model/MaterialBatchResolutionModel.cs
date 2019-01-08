using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.Model
{
    public class MaterialBatchResolutionModel
    {
        public int MaterialId { get; set; }

        public string MaterialName { get; set; }

        public string UnitSymbol { get; set; }

        public decimal Amount { get; set; }

        public string BatchNumber { get; set; }
    }
}
