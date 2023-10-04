using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mtecl.ApiClientBuilder;
using SmartEmailingApi.Client.Messages;

namespace SmartEmailingApi.Client
{
    public interface IContacts
    {
        /// <summary>
        /// Get Contacts
        /// </summary>
        /// <param name="limit">Number of records per page. Maximum allowed value is 500</param>
        /// <param name="offset">Skip given number of records to allow pagination</param>
        /// <param name="select">Comma separated list of properties to select. eg. ?select=id,emailaddress If not provided, all fields are selected</param>
        /// <param name="expand">Using this parameter customfields_url property will be replaced by customfields contianing expanded data. See examples below Prepend - to any key for desc direction, eg. ?expand=customfields. For more information see /contact-customfields endpoint.</param>
        /// <param name="sort">Comma separated list of sorting keys from left side. Prepend - to any key for desc direction, eg. ?sort=emailaddress,-name,id</param>
        /// <param name="filter">As a parameter name, you can use any combination of these properties to filter your results. To select everyone named Johnny and living in Japan use this query params: ?name=Johnny&country=Japan</param>
        /// <returns></returns>
        [Get("/api/v3/contacts")]
        Task<DataResponse<List<Contact>>> Get(
            [Query]
            int limit = 500,

            [Query]
            int offset = 0,

            [Query]
            ColumnList select = null,

            [Query]
            ColumnList expand = null,

            [Query]
            ColumnList sort = null,

            [Query]
            Dictionary<string, string> filter = null);

        [Get("/api/v3/contacts/:contactId")]
        Task<DataResponse<Contact>> GetSingle(int contactId, [Query] ColumnList select = null,
            [Query] ColumnList expand = null);

        [Post("/api/v3/change-emailaddress")]
        Task<SeResponse> ChangeEmailAddress(string from, string to);

        [Post("/api/v3/contacts/forget/:contactId")]
        Task<SeResponse> ForgetContact(int contactId);
    }
}
