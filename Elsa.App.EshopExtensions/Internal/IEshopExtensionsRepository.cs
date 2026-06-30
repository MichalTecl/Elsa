using System.Collections.Generic;

using Elsa.App.EshopExtensions.Model;

namespace Elsa.App.EshopExtensions.Internal
{
    public interface IEshopExtensionsRepository
    {
        EshopExtensionsStatus GetStatus();
        List<CouponValidationRuleListItemModel> GetCouponRules();
        CouponValidationRuleEditorModel GetCouponRule(int? ruleId);
        CouponValidationRuleEditorModel SaveCouponRule(CouponValidationRuleEditorModel model);
        void DeleteCouponRule(int ruleId);
    }
}
