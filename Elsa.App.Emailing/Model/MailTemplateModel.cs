using System;

namespace Elsa.App.Emailing.Model
{
    public class MailTemplateModel
    {
        public int? Id { get; set; }

        public string TypeName { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }

        public DateTime? LastChangeDt { get; set; }

        public string LastChangeUserName { get; set; }
    }
}
