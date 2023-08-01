using Elsa.Common.Interfaces;
using Elsa.Users.Infrastructure;

namespace Elsa.Apps.CommonData
{
    [UserRights]
    public static class CommonDataUserRights
    {
        public static readonly UserRight ViewSystemWidget = new UserRight(nameof(ViewSystemWidget), "Systém");
        public static readonly UserRight DataApp = new UserRight(nameof(DataApp), "Aplikace Data", ViewSystemWidget);
        public static readonly UserRight UsersApp = new UserRight(nameof(UsersApp), "Uživatelé, Role ...", ViewSystemWidget);
        public static readonly UserRight SettingsApp = new UserRight(nameof(SettingsApp), "Nastavení", ViewSystemWidget);
    }
}
