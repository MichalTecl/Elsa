using Elsa.Common.Interfaces;
using Elsa.Users.Infrastructure;

namespace Elsa.App.Crm
{
    [UserRights]
    public static class CrmUserRights
    {
        public static readonly UserRight ViewCrmWidget = new UserRight(nameof(ViewCrmWidget), "CRM");
        public static readonly UserRight DistributorsApp = new UserRight(nameof(DistributorsApp), "Velkoodběratelé - přístup do aplikace", ViewCrmWidget);        
    }
}
