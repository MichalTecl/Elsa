using Elsa.Integration.Crm.Raynet.Model;
using System.Collections.Generic;

namespace Elsa.Integration.Crm.Raynet
{
    public interface IRaynetClient
    {
        RnResponse<IdResponse> InsertContact(ContactDetail contact);
        RnResponse<IdResponse> UpdateContact(long id, ContactDetail c);
        RnResponse<IdResponse> UpdateContact(ContactDetail c);
        RnResponse<List<Contact>> GetContacts(int offset = 0, int limit = 1000, string regNumber = null, string name = null, string fulltext = null);
        RnResponse<List<CompanyCategory>> GetCompanyCategories();
        RnResponse<IdResponse> CreateBusinessCase(BusinessCaseModel bc);
        RnResponse ChangeBcValidity(long bcId, bool isValid);
        RnResponse<ContactDetail> GetContactDetail(long contactId);
        RnResponse<IdResponse> AddContactAddress(long contactId, AddressBucket address);
        RnResponse<IdResponse> UpdateContactAddress(long contactId, long addressId, AddressBucket address);
        RnResponse<List<ProductListItem>> GetProductList();
        RnResponse<List<PersonInfo>> GetPersons(string email);
        RnResponse DeleteBusinessCase(long bcaseId);
        RnResponse DeleteContact(long companyId);
        RnResponse<List<ContactSource>> GetContactSources();
        RnResponse<long> CreateContactSource(string code);
        RnResponse<List<BusinessCaseModel>> GetBusinessCases(string name = null);
    }
}
