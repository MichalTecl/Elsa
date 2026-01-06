using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Integration.SmartEmailing.Messages;
using Mtecl.ApiClientBuilder;

namespace Elsa.Integration.SmartEmailing
{
    // TODO should be possible to put /api/v3/contactlists to interface level?
    public interface IContactlists
    {
        [Get("/api/v3/contactlists/:contactlistId/added-contacts")]
        Task<DataResponse<ItemsCount>> GetAddedContactsCount(int contactlistId);

        [Post("/api/v3/contactlists")]
        Task<DataResponse<ContactlistData>> Create(ContactlistCreationRequest data);

        [Get("/api/v3/contactlists/:contactlistId/distribution")]
        Task<DataResponse<DistributionData>> GetDistribution(int contactlistId);

        [Get("/api/v3/contactlists")]
        Task<DataResponse<List<ContactlistData>>> Get([Query]ColumnList select = null, [Query]int limit = 500, [Query]int offset = 0);

        [Get("/api/v3/contactlists/:contactlistId")]
        Task<DataResponse<ContactlistData>> GetSingle(int contactlistId);

        [Get("/api/v3/contactlists/:contactlistId/truncate")]
        Task<SeResponse> Truncate(int contactlistId);

        [Put("/api/v3/contactlists/:id")]
        Task<DataResponse<ContactlistData>> Update(int contactlistId, ContactlistCreationRequest data);

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
        [Get("/api/v3/contactlists/:contactlistId/contacts")]
        Task<DataResponse<List<Contact>>> GetContacts(

            int contactlistId,

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
        [Get("/api/v3/contactlists/:contactlistId/contacts/confirmed")]
        Task<DataResponse<List<Contact>>> GetConfirmedContacts(

            int contactlistId,

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
        [Get("/api/v3/contactlists/:contactlistId/contacts/unsubscribed")]
        Task<DataResponse<List<Contact>>> GetUnsubscribedContacts(

            int contactlistId,

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
    }
}
