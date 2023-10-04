using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SmartEmailingApi.Client.Messages
{
    public class Contact
    {
        /// <summary>
        /// Unique ID of Contact
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// UID of Contact
        /// </summary>
        [JsonPropertyName("guid")]
        public string Guid { get; set; }

        /// <summary>
        /// Email address of Contact. Must be unique.
        /// </summary>
        [JsonPropertyName("emailaddress")]
        public string EmailAddress { get; set; }

        /// <summary>
        /// First name
        /// </summary>
        [JsonPropertyName("name")]
        public string FirstName { get; set; }

        /// <summary>
        /// Last name
        /// </summary>
        [JsonPropertyName("surname")]
        public string LastName { get; set; }

        /// <summary>
        /// Titles before name
        /// </summary>
        [JsonPropertyName("titlesbefore")]
        public string TitlesBefore { get; set; }

        /// <summary>
        /// Titles after name
        /// </summary>
        [JsonPropertyName("titlesafter")]
        public string TitlesAfter { get; set; }

        /// <summary>
        /// Salutation (by first name)
        /// </summary>
        [JsonPropertyName("salution")]
        public string Salutation { get; set; }

        /// <summary>
        /// Company
        /// </summary>
        [JsonPropertyName("company")]
        public string Company { get; set; }

        /// <summary>
        /// Street
        /// </summary>
        [JsonPropertyName("street")]
        public string Street { get; set; }

        /// <summary>
        /// Town
        /// </summary>
        [JsonPropertyName("town")]
        public string Town { get; set; }

        /// <summary>
        /// Postal/ZIP code
        /// </summary>
        [JsonPropertyName("postalcode")]
        public string PostalCode { get; set; }

        /// <summary>
        /// Country
        /// </summary>
        [JsonPropertyName("country")]
        public string Country { get; set; }

        /// <summary>
        /// Cellphone number
        /// </summary>
        [JsonPropertyName("cellphone")]
        public string Cellphone { get; set; }

        /// <summary>
        /// Phone number
        /// </summary>
        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        /// <summary>
        /// Language in POSIX format, eg. cz_CZ
        /// </summary>
        [JsonPropertyName("language")]
        public string Language { get; set; }

        /// <summary>
        /// Custom notes
        /// </summary>
        [JsonPropertyName("notes")]
        public string Notes { get; set; }

        /// <summary>
        /// Gender
        /// </summary>
        [JsonPropertyName("gender")]
        public string Gender { get; set; }

        /// <summary>
        /// Date and time of Contact creation in YYYY-MM-DD HH:MM:SS format
        /// </summary>
        [JsonPropertyName("created")]
        public string Created { get; set; }

        /// <summary>
        /// Date and time of update Contact's properties (Customfields and list do not change this value) in YYYY-MM-DD HH:MM:SS format.
        /// </summary>
        [JsonPropertyName("update")]
        public string Update { get; set; }

        /// <summary>
        /// Date and time of Contact's last click in one of your emails in YYYY-MM-DD HH:MM:SS format.
        /// </summary>
        [JsonPropertyName("last_clicked")]
        public string _LastClicked { get; set; }
        
        /// <summary>
        /// Date and time of Contact's last open of one of your emails in YYYY-MM-DD HH:MM:SS format.
        /// </summary>
        [JsonPropertyName("last_opened")]
        public string LastOpened { get; set; }

        /// <summary>
        /// Time of Contact's prefered delivery time based on it's opening history in YYYY-MM-DD HH:MM:SS format. Date does not matter.
        /// </summary>
        [JsonPropertyName("preferredDeliveryTime")]
        public string PreferredDeliveryTime { get; set; }

        /// <summary>
        /// Count of softbounces in a row. Open or click in email resets this counter back to 0.
        /// </summary>
        [JsonPropertyName("softbounced")]
        public int SoftBounced { get; set; }

        /// <summary>
        /// 0 if Contact is OK, 1 if Contact has permanent delivery failure.
        /// </summary>
        [JsonPropertyName("hardbounced")]
        public int HardBounced { get; set; }

        /// <summary>
        /// 0 if Contact is OK, 1 if Contact does not want any of your e-mails anymore.
        /// </summary>
        [JsonPropertyName("blacklisted")]
        public int Blacklisted { get; set; }

        /// <summary>
        /// Date of Contact's nameday in YYYY-MM-DD 00:00:00 format.
        /// </summary>
        [JsonPropertyName("nameday")]
        public string NameDay { get; set; }

        /// <summary>
        /// Date of Contact's birthday in YYYY-MM-DD 00:00:00 format.
        /// </summary>
        [JsonPropertyName("birthday")]
        public string Birthday { get; set; }

        /// <summary>
        /// URL of Customfield values endpoint. Can be expanded into customfields property.
        /// </summary>
        [JsonPropertyName("customfields_url")]
        public string CustomFieldsUrl { get; set; }

        ///// <summary>
        ///// Contactlists collection.
        ///// </summary>
        //[JsonPropertyName("contactlists")]
        //public ContactList[] ContactLists { get; set; }

        /// <summary>
        /// Status shortcode ok.
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; }
    }
}