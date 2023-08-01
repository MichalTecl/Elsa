using Elsa.Common.Interfaces;
using Elsa.Users.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Commerce
{
    [UserRights]
    public static class OrdersOverviewUserRights
    {
        public static readonly UserRight ViewOrdersOverviewWidget = new UserRight(nameof(ViewOrdersOverviewWidget), "Přehled objednávek");
        public static readonly UserRight ShowOrdersSummary = new UserRight(nameof(ShowOrdersSummary), "Zobrazit tabulku počtů objednávek", ViewOrdersOverviewWidget);
        public static readonly UserRight OpenPaymentPairingApp = new UserRight(nameof(OpenPaymentPairingApp), "Aplikace pro ruční párování plateb", ViewOrdersOverviewWidget);
        public static readonly UserRight AllowManualPaymentPairing = new UserRight(nameof(AllowManualPaymentPairing), "Může párovat platby", OpenPaymentPairingApp);        
    }
}
