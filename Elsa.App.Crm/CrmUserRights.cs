using Elsa.Common.Interfaces;
using Elsa.Users.Infrastructure;

namespace Elsa.App.Crm
{
    [UserRights]
    public static class CrmUserRights
    {
        public static readonly UserRight ViewCrmWidget = new UserRight(nameof(ViewCrmWidget), "CRM");
        public static readonly UserRight DistributorsApp = new UserRight(nameof(DistributorsApp), "Velkoodběratelé - přístup do aplikace", ViewCrmWidget);
        public static readonly UserRight DistributorsAppEdits = new UserRight(nameof(DistributorsAppEdits), "Velkoodběratelé - úpravy", DistributorsApp);
        public static readonly UserRight EmailConversationsPreview = new UserRight(nameof(EmailConversationsPreview), "E-mailové konverzace - vidí AI sumarizace", DistributorsApp);
        public static readonly UserRight EmailConversationsFull = new UserRight(nameof(EmailConversationsFull), "E-mailové konverzace - vidí plný text", EmailConversationsPreview);
        public static readonly UserRight EmailSummaryPromptEdit = new UserRight(nameof(EmailSummaryPromptEdit), "E-mailové konverzace - ladění promptu AI sumarizace", EmailConversationsFull); 
    }
}
