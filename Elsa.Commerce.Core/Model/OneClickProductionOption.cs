using Elsa.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.Model
{
    public class OneClickProductionOption
    {
        public string BatchNumber { get; set; }
        public int SourceMaterialId { get; set; }
        public int RecipeId { get; set; }
        public string ProducedMaterialName { get; set; }
        public int ProducedMaterialId { get; set; }
        public string VisibleForUserRole { get; set; }
        public string ProducibleAmountUnit { get; set; }
        public decimal ProducibleAmount { get; set; }
        public string OptionKey => $"{RecipeId}_{SourceMaterialId}_{BatchNumber}";
        public string Text => $"Vyrobit {StringUtil.FormatDecimal(ProducibleAmount)}{ProducibleAmountUnit} {ProducedMaterialName}";
    }
}
