namespace Elsa.App.Crm.Model
{
    public class PromptInfo
    {
        public int PromptId { get; set; }
        public string Author { get; set; }
        public string CreateDt { get; set; }
        public string Prompt { get; set; }
        public bool IsActive { get; set; }
        public bool CanDelete { get; set; }
    }
}
