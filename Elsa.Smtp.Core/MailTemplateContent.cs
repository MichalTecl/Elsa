namespace Elsa.Smtp.Core
{
    public class MailTemplateContent
    {
        public MailTemplateContent(string subject, string body)
        {
            Subject = subject;
            Body = body;
        }

        public string Subject { get; }

        public string Body { get; }
    }
}
