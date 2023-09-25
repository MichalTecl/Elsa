using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Elsa.Integration.Crm.Raynet;
using Elsa.Integration.Crm.Raynet.Model;

namespace Elsa.Integration.Crm.Raynet
{
    public class RnActions : IRaynetClient
    {
        private readonly RnProtocol _protocol;

        public RnActions(RnProtocol client)
        {
            _protocol = client;
        }

        public RnResponse<IdResponse> InsertContact(Contact contact)
        {
            return _protocol.Call<RnResponse<IdResponse>>(HttpMethod.Put,
                "https://app.raynet.cz/api/v2/company/",
                payload: contact);
        }

        public RnResponse<IdResponse> UpdateContact(long id, Contact c)
        {
            return _protocol.Call<RnResponse<IdResponse>>(HttpMethod.Post, $"https://app.raynet.cz/api/v2/company/{id}/",
                payload: c);
        }

        public RnResponse<List<Contact>> GetContacts(int offset = 0, int limit = 1000)
        {
            return _protocol.Call<RnResponse<List<Contact>>>(HttpMethod.Get, $"https://app.raynet.cz/api/v2/company/?offset={offset}&limit={limit}");
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

        public RnResponse<Contact> GetContactDetail(long contactId)
        {
            return _protocol.Call<RnResponse<Contact>>(HttpMethod.Get, $"https://app.raynet.cz/api/v2/company/{contactId}/");
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
    }
}
