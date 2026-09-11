using System.Collections.Generic;

namespace Elsa.App.Emailing.Model
{
    public class MailTemplateModel
    {
        public string TypeName { get; set; }

        public string OriginalTypeName { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }

        public string BodyBase64 { get; set; }

        public string BodyFormat { get; set; }

        public string OriginalBodyFormat { get; set; }
    }

    public class MailTemplateTestSettings
    {
        public string Recipient { get; set; }
    }

    public class MailTemplateTestRequest
    {
        public string MailboxType { get; set; }

        public string Recipient { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }

        public string BodyFormat { get; set; }

        public Dictionary<string, string> Values { get; set; }
    }
}
