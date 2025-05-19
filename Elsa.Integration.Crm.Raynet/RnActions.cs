using Elsa.Integration.Crm.Raynet;
using Elsa.Integration.Crm.Raynet.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace Elsa.Integration.Crm.Raynet
{
    public class RnActions : IRaynetClient
    {
        private readonly RnProtocol _protocol;

        private readonly Lazy<Dictionary<string, List<CustomFieldDefinitionModel>>> _cfDefinitions;

        public RnActions(RnProtocol client)
        {
            _protocol = client;

            _cfDefinitions = new Lazy<Dictionary<string, List<CustomFieldDefinitionModel>>>(() => GetCustomFieldTypes().Data);
        }

        public RnResponse<IdResponse> InsertContact(ContactDetail contact)
        {
            return _protocol.Call<RnResponse<IdResponse>>(HttpMethod.Put,
                "https://app.raynet.cz/api/v2/company/",
                payload: contact);
        }

        public RnResponse<IdResponse> UpdateContact(long id, ContactDetail c)
        {
            return _protocol.Call<RnResponse<IdResponse>>(HttpMethod.Post, $"https://app.raynet.cz/api/v2/company/{id}/",
                payload: c);
        }

        public RnResponse<IdResponse> UpdateContact(ContactDetail c)
        {
            return UpdateContact(c.Id.Value, c);
        }

        public RnResponse<List<Contact>> GetContacts(int offset = 0, int limit = 1000, string regNumber = null, string name = null, string fulltext = null)
        {
            var url = $"https://app.raynet.cz/api/v2/company/?offset={offset}&limit={limit}";

            if (!string.IsNullOrWhiteSpace(regNumber))
                url += $"&regNumber={HttpUtility.UrlEncode(regNumber)}";

            if (!string.IsNullOrWhiteSpace(name))
                url += $"&name[LIKE_NOCASE]={HttpUtility.UrlEncode(name)}";

            if (!string.IsNullOrWhiteSpace(fulltext))
                url += $"&fulltext={HttpUtility.UrlEncode(fulltext)}";

            return _protocol.Call<RnResponse<List<Contact>>>(HttpMethod.Get, url);
        }

        public RnResponse<List<CompanyCategory>> GetCompanyCategories()
        {
            return _protocol.Call<RnResponse<List<CompanyCategory>>>(HttpMethod.Get, "https://app.raynet.cz/api/v2/companyCategory/");
        }

        public RnResponse<IdResponse> CreateBusinessCase(BusinessCaseModel bc)
        {
            return _protocol.Call<RnResponse<IdResponse>>(HttpMethod.Put, "https://app.raynet.cz/api/v2/businessCase/createWithItems/", payload: bc);
        }

        public RnResponse ChangeBcValidity(long businessCaseId, bool isValid)
        {
            return _protocol.Call<RnResponse>(HttpMethod.Post, $"https://app.raynet.cz/api/v2/businessCase/{businessCaseId}/{(isValid ? "valid" : "invalid")}");
        }

        public RnResponse<ContactDetail> GetContactDetail(long contactId)
        {
            return _protocol.Call<RnResponse<ContactDetail>>(HttpMethod.Get, $"https://app.raynet.cz/api/v2/company/{contactId}/");
        }

        public RnResponse<IdResponse> AddContactAddress(long contactId, AddressBucket address)
        {
            return _protocol.Call<RnResponse<IdResponse>>(HttpMethod.Put, $"https://app.raynet.cz/api/v2/company/{contactId}/address/", payload: address);
        }

        public RnResponse<IdResponse> UpdateContactAddress(long contactId, long addressId, AddressBucket address)
        {
            address.Id = addressId;
            return _protocol.Call<RnResponse<IdResponse>>(HttpMethod.Post, $"https://app.raynet.cz/api/v2/company/{contactId}/address/{addressId}/", payload: address);
        }

        public RnResponse<List<ProductListItem>> GetProductList()
        {
            return _protocol.Call<RnResponse<List<ProductListItem>>>(HttpMethod.Get, "https://app.raynet.cz/api/v2/product/");
        }

        public RnResponse<List<PersonInfo>> GetPersons(string email)
        {
            return _protocol.Call<RnResponse<List<PersonInfo>>>(HttpMethod.Get, $"https://app.raynet.cz/api/v2/person/?contactInfo.email[LIKE_NOCASE]={email}%");
        }

        public RnResponse DeleteBusinessCase(long bcaseId)
        {
            return _protocol.Call<RnResponse>(HttpMethod.Delete, $"https://app.raynet.cz/api/v2/businessCase/{bcaseId}/");
        }

        public RnResponse DeleteContact(long companyId)
        {
            return _protocol.Call<RnResponse>(HttpMethod.Delete, $"https://app.raynet.cz/api/v2/company/{companyId}/");
        }

        public RnResponse<List<ContactSource>> GetContactSources()
        {
            return _protocol.Call<RnResponse<List<ContactSource>>>(HttpMethod.Get, $"https://app.raynet.cz/api/v2/contactSource/");
        }

        public RnResponse<long> CreateContactSource(string code)
        {
            return _protocol.Call<RnResponse<long>>(HttpMethod.Put, "https://app.raynet.cz/api/v2/contactSource/", payload: new { code01 = code });
        }

        public RnResponse<List<BusinessCaseModel>> GetBusinessCases(string name = null)
        {
            var url = "https://app.raynet.cz/api/v2/businessCase/";

            if (!string.IsNullOrWhiteSpace(name))
                url = $"{url}?name[LIKE_NOCASE]={HttpUtility.UrlEncode(name)}";

            return _protocol.Call<RnResponse<List<BusinessCaseModel>>>(HttpMethod.Get, url);
        }

        public RnResponse<Dictionary<string, List<CustomFieldDefinitionModel>>> GetCustomFieldTypes()
        {
            return _protocol.Call<RnResponse<Dictionary<string, List<CustomFieldDefinitionModel>>>>(HttpMethod.Get, "https://app.raynet.cz/api/v2/customField/config/");
        }

        public T ReadCustomField<T>(IHasCustomFields entity, string customFieldLabel) 
        {
            if (!_cfDefinitions.Value.TryGetValue(entity.CustomFieldsCategory, out var definitions))
                throw new ArgumentException($"No custom fields category \"{entity.CustomFieldsCategory}\"");

            var definition = definitions.FirstOrDefault(d => d.Label == customFieldLabel);
            if (definition == null)
                throw new ArgumentException($"No custom field has label \"{customFieldLabel}\"");

            if (!entity.CustomFields.TryGetValue(definition.Name, out var value))
                return default;

            return (value is T converted) ? converted : default;                
        }

        public RnResponse<List<Activity>> GetActivities(int offset = 0, int limit = 1000)
        {
            var url = $"https://app.raynet.cz/api/v2/activity/?offset={offset}&limit={limit}";

            return _protocol.Call<RnResponse<List<Activity>>>(HttpMethod.Get, url);
        }
    }
}
