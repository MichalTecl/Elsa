using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SmartEmailingApi.Client.Messages
{
    public class ContactlistData : ContactlistCreationRequest
    {
        /// <summary>
        /// Contactlist ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Percentage of link-clicking contacts in list. null if there are no data to calculate
        /// </summary>
        public int? ClickRate { get; set; }

        /// <summary>
        /// Percentage of email-opening contacts in list. null if there are no data to calculate
        /// </summary>
        public int? OpenRate { get; set; }

        /// <summary>
        /// Total count of contacts in list.
        /// </summary>
        [JsonPropertyName("alertOut")]
        public int ContactsCount { get; set; }

        /// <summary>
        /// ID of supervising Segment. null if there is none
        /// </summary>
        [JsonPropertyName("segment_id")]
        public int? SegmentId { get; set; }

        [JsonPropertyName("created")]
        public string _Created { get; set; }

        [JsonIgnore]
        public DateTime Created => DateTime.ParseExact(_Created, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        //TODO why Signature and Notes are not in creaton request?
        /// <summary>
        /// This can be used as customfield in your e-mails.
        /// </summary>
        public string Signature { get; set; }

        /// <summary>
        /// Custom contactlist notes
        /// </summary>
        public string Notes { get; set; }

    }
}
