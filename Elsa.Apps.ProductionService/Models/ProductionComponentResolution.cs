using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Commerce.Core;
using Elsa.Common;

namespace Elsa.Apps.ProductionService.Models
{
    public class ProductionComponentResolution
    {
        private decimal m_amount = 0;
        private string m_unit = string.Empty;
        private Amount m_calculatedAmount = null;

        public string BatchNumber { get; set; }

        public decimal Amount
        {
            get => m_amount;
            set
            {
                m_amount = value;
                m_calculatedAmount = null;
            }
        }

        public string UnitSymbol
        {
            get => m_unit;
            set
            {
                m_unit = value;
                m_calculatedAmount = null;
            }
        }

        public string BatchCreationDt { get; set; }

        public string BatchAvailableAmountText { get; set; }

        public decimal BatchAvailableAmount { get; set; }
        public long Sorter { get; set; }

        public string Key { get; set; }

        internal Amount GetAmount(IUnitRepository ur)
        {
            if (m_calculatedAmount == null)
            {
                if (string.IsNullOrWhiteSpace(UnitSymbol))
                {
                    return null;
                }

                m_calculatedAmount = new Amount(Amount, ur.GetUnitBySymbol(UnitSymbol));
            }

            return m_calculatedAmount;
        }
    }
}
