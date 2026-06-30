using System;
using System.Collections.Generic;

namespace Elsa.App.EshopExtensions.Model
{
    public class CouponValidationRuleEditorModel
    {
        public int? Id { get; set; }
        public string RuleName { get; set; }
        public bool IsActive { get; set; }
        public string ValidationMessage { get; set; }
        public List<string> CouponCodes { get; set; }
        public List<Rule> Rules { get; set; }
    }
}
