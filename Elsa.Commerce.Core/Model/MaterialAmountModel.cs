using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common;

namespace Elsa.Commerce.Core.Model
{
    public class MaterialAmountModel
    {
        public MaterialAmountModel(int materialId, Amount amount)
        {
            MaterialId = materialId;
            Amount = amount;
        }

        public int MaterialId { get; }

        public Amount Amount { get; }
    }
}
