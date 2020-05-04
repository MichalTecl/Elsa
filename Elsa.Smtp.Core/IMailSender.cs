using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Smtp.Core
{
    public interface IMailSender
    {
        void Send(string to, string subject, string body, params string[] attachmentFiles);

        void SendToGroup(string groupName, string subject, string body, params string[] attachmentFiles);
    }
}
