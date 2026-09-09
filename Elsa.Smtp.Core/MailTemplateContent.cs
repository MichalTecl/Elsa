namespace Elsa.Smtp.Core
{
    public static class MailTemplateBodyFormats
    {
        public const string PlainText = "PlainText";

        public const string Html = "Html";
    }

    public class MailTemplateContent
    {
        public MailTemplateContent(string subject, string body)
            : this(subject, body, false)
        {
        }

        public MailTemplateContent(string subject, string body, bool isHtml)
        {
            Subject = subject;
            Body = body;
            IsHtml = isHtml;
        }

        public string Subject { get; }

        public string Body { get; }

        public bool IsHtml { get; }
    }
}
