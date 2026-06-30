using System.Collections.Generic;

using Elsa.App.EshopExtensions.Internal;
using Elsa.App.EshopExtensions.Model;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;

using Robowire.RoboApi;

namespace Elsa.App.EshopExtensions.Controllers
{
    [Controller("discountCoupons")]
    public class DiscountCouponsController : ElsaControllerBase
    {
        private readonly IEshopExtensionsRepository _repository;

        public DiscountCouponsController(IWebSession webSession, ILog log, IEshopExtensionsRepository repository)
            : base(webSession, log)
        {
            _repository = repository;
        }

        public List<CouponValidationRuleListItemModel> GetCouponRules()
        {
            EnsureUserRight(EshopExtensionsUserRights.DiscountCouponsApp);
            return _repository.GetCouponRules();
        }

        public CouponValidationRuleEditorModel GetCouponRule(int? ruleId)
        {
            EnsureUserRight(EshopExtensionsUserRights.DiscountCouponsApp);
            return _repository.GetCouponRule(ruleId);
        }

        public CouponValidationRuleEditorModel SaveCouponRule(CouponValidationRuleEditorModel model)
        {
            EnsureUserRight(EshopExtensionsUserRights.DiscountCouponsApp);
            return _repository.SaveCouponRule(model);
        }

        public List<CouponValidationRuleListItemModel> DeleteCouponRule(int ruleId)
        {
            EnsureUserRight(EshopExtensionsUserRights.DiscountCouponsApp);
            _repository.DeleteCouponRule(ruleId);
            return _repository.GetCouponRules();
        }
    }
}
