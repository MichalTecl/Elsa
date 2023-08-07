using Elsa.Common.Interfaces;
using Elsa.Users.Infrastructure;

namespace Elsa.Apps.Reporting
{
    [UserRights]
    public static class ReportingUserRights
    {
        public static readonly UserRight ViewReportingWidget = new UserRight(nameof(ViewReportingWidget), "Reporty");
        public static readonly UserRight ReportingApp = new UserRight(nameof(ReportingApp), "Aplikace Reporty", ViewReportingWidget);
        public static readonly UserRight InspectorApp = new UserRight(nameof(InspectorApp), "Inspektor", ViewReportingWidget);
        public static readonly UserRight InspectorActions = new UserRight(nameof(InspectorActions), "Inspektor - Akce", InspectorApp);

        public static readonly UserRight DownloadInvoicingFormPackages = new UserRight(nameof(DownloadInvoicingFormPackages), "Stahování balíčků účetních dat");
    }
}
