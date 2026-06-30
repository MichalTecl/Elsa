using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.App.EshopExtensions.Model
{
    public class CouponValidationRule
    {
        public List<string> CouponCodes { get; set; }

        public List<Rule> Rules { get; set; }
    }

    public class Rule
    {
        public string MustHaveProductInCart { get; set; }

        public int MinQuantity { get; set; }

        public int MaxQuantity { get; set; }

        public string ViolationMessage { get; set; }

        public Rule AndAlso { get; set; }
    }

    
}
