using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Elsa.Integration.SmartEmailing.Messages
{
    public class ContactlistCreationRequest
    {
        /// <summary>
        /// Contactlist name that will not be displayed to public
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Contactlist name that will be displayed to public
        /// </summary>
        [JsonPropertyName("publicname")]
        public string PublicName { get; set; }

        /// <summary>
        /// Name of contact list owner, will be used in your campaigns. Required if replyto or senderemail is provided.
        /// </summary>
        [JsonPropertyName("sendername")]
        public string SenderName { get; set; }

        /// <summary>
        /// E-mail address of list owner, will be used in your campaigns. Must be a confirmed e-mail. Required if replyto or sendername is provided.
        /// </summary>
        [JsonPropertyName("senderemail")]
        public string SenderEmail { get; set; }

        /// <summary>
        ///Reply-to e-mail address of list owner, will be used in your campaigns.Must be a confirmed e-mail.Required if senderemail or sendername is provided.
        /// </summary>
        [JsonPropertyName("replyto")]
        public string ReplyTo { get; set; }
    }
}
