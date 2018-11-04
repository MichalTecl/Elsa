﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.Common
{
    public class Amount
    {
        public Amount(decimal value, IMaterialUnit unit)
        {
            Value = value;
            Unit = unit;
        }

        public decimal Value { get; }
        public IMaterialUnit Unit { get; }

        /*
        public override bool Equals(object obj)
        {
            var b = obj as Amount;
            if (b == null)
            {
                return false;
            }

            return 
        }
        */

        public override string ToString()
        {
            return $"{StringUtil.FormatDecimal(Value)} {Unit.Symbol}";
        }
    }
}
