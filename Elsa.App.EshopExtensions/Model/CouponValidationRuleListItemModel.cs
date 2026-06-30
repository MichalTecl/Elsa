namespace Elsa.App.EshopExtensions.Model
{
    public class CouponValidationRuleListItemModel
    {
        public int Id { get; set; }
        public string RuleName { get; set; }
        public bool IsActive { get; set; }
        public string StatusText { get; set; }
        public string CouponCodesPreview { get; set; }
        public int CouponCodesCount { get; set; }
    }
}
