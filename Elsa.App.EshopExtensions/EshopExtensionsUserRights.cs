using Elsa.Common.Interfaces;
using Elsa.Users.Infrastructure;

namespace Elsa.App.EshopExtensions
{
    [UserRights]
    public static class EshopExtensionsUserRights
    {
        public static readonly UserRight ViewEshopExtensionsWidget = new UserRight(nameof(ViewEshopExtensionsWidget), "Integrace E-Shopu");
        public static readonly UserRight DiscountCouponsApp = new UserRight(nameof(DiscountCouponsApp), "Úpravy slevových kuponů", ViewEshopExtensionsWidget);
    }
}
