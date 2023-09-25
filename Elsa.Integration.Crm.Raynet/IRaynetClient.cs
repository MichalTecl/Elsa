﻿using Elsa.Integration.Crm.Raynet.Model;
using System.Collections.Generic;

namespace Elsa.Integration.Crm.Raynet
{
    public interface IRaynetClient
    {
        RnResponse<IdResponse> InsertContact(Contact contact);
        RnResponse<IdResponse> UpdateContact(long id, Contact c);
        RnResponse<List<Contact>> GetContacts(int offset = 0, int limit = 1000);
        RnResponse<List<CompanyCategory>> GetCompanyCategories();
        RnResponse<IdResponse> CreateBusinessCase(BusinessCaseModel bc);
        RnResponse ChangeBcValidity(long bcId, bool isValid);
        RnResponse<Contact> GetContactDetail(long contactId);
        RnResponse<IdResponse> AddContactAddress(long contactId, AddressBucket address);
        RnResponse<IdResponse> UpdateContactAddress(long contactId, long addressId, AddressBucket address);
        RnResponse<List<ProductListItem>> GetProductList();
    }
}
