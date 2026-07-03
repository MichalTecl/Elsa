using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace Elsa.App.EshopExtensions.Model
{
    public class CouponValidationRule
    {
        public List<string> CouponCodes { get; set; }

        public List<Rule> Rules { get; set; }

        internal CouponValidationRule ToHashedForm()
        {
            return new CouponValidationRule
            {
                Rules = Rules?.Select(CloneRule).ToList() ?? new List<Rule>(),
                CouponCodes = (CouponCodes ?? new List<string>()).Select(Hash).ToList()
            };
        }

        private static Rule CloneRule(Rule source)
        {
            if (source == null)
            {
                return null;
            }

            return new Rule
            {
                ConditionType = source.ConditionType,
                MustHaveProductsInCart = source.MustHaveProductsInCart?.ToList(),
                MinQuantity = source.MinQuantity,
                MaxQuantity = source.MaxQuantity,
                MinOrderPrice = source.MinOrderPrice,
                ViolationMessage = source.ViolationMessage,
                AndAlso = CloneRule(source.AndAlso)
            };
        }

        private static string Hash(string str)
        {
            var source = str ?? string.Empty;
            var hash = 0;

            for (var i = 0; i < source.Length; i++)
            {
                hash = ((hash << 5) - hash) + source[i];
            }

            return unchecked((uint)hash).ToString("x8");
        }
    }

    public class Rule
    {
        public string ConditionType { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string> MustHaveProductsInCart { get; set; }

        public int MinQuantity { get; set; }

        public int MaxQuantity { get; set; }

        public decimal? MinOrderPrice { get; set; }

        public string ViolationMessage { get; set; }

        public Rule AndAlso { get; set; }
    }

    public static class CouponRuleConditionTypes
    {
        public const string ProductsInCart = "productsInCart";
        public const string MinimumOrderPrice = "minimumOrderPrice";
    }
}
