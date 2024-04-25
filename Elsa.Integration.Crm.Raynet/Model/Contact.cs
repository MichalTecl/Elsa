namespace Elsa.Integration.Crm.Raynet.Model
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AddressBucket
    {
        public long? Id { get; set; }

        public bool? Primary { get; set; }

        public Address Address { get; set; }

        public ContactInfo ContactInfo { get; set; }

        public bool IsSameAs(AddressBucket addressToSave)
        {
            return (addressToSave != null)
                && (addressToSave.Primary == Primary)
                && (addressToSave.ContactInfo?.Email == ContactInfo?.Email)
                && (addressToSave.ContactInfo?.Tel1 == ContactInfo?.Tel1)
                && (addressToSave.Address != null)
                && (Address != null)
                && (addressToSave.Address.Name == Address.Name)
                && (addressToSave.Address.Street == Address.Street)
                && (addressToSave.Address.City == Address.City)
                && (addressToSave.Address.Province == Address.Province)
                && (addressToSave.Address.ZipCode == Address.ZipCode)
                && (addressToSave.Address.Country == Address.Country);
        }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Address
    {
        public long? Id { get; set; }

        public string Name { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string ZipCode { get; set; }
        public string Country { get; set; }
        public double? Lat { get; set; }
        public double? Lng { get; set; }
        public ContactInfo ContactInfo { get; set; }
        public int Territory { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ContactInfo
    {
        public string Email { get; set; }
        public string Tel1 { get; set; }
        public string Tel1Type { get; set; }
        public string Www { get; set; }
        public bool DoNotSendMM { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ContactDetail : Contact, IHasCustomFields
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string TitleBefore { get; set; }
        public string TitleAfter { get; set; }
        public string Salutation { get; set; }
        public IdContainer EmployeesNumber { get; set; }
        public IdContainer LegalForm { get; set; }
        public IdContainer PaymentTerm { get; set; }
        public IdContainer Turnover { get; set; }
        public IdContainer EconomyActivity { get; set; }
        public IdContainer CompanyClassification1 { get; set; }
        public IdContainer CompanyClassification2 { get; set; }
        public IdContainer CompanyClassification3 { get; set; }
        public string BankAccount { get; set; }
        public string Databox { get; set; }
        public string Court { get; set; }
        public string Birthday { get; set; }
        public List<AddressBucket> Addresses { get; set; }
        public List<string> Tags { get; set; }
        public Dictionary<string, object> CustomFields { get; set; }
        public Dictionary<string, string> SocialNetworkContact { get; set; }
        public IdContainer SecurityLevel { get; set; }
        public IdContainer OriginLead { get; set; }

        public string CustomFieldsCategory => "Company";
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Contact
    {
        public long? Id { get; set; }
        public string Name { get; set; }
        public bool Person { get; set; } = false;
        public IdContainer Owner { get; set; }
        public string Rating { get; set; } = "A";
        public string State { get; set; } = "B_ACTUAL";
        public string Role { get; set; } = "A_SUBSCRIBER";
        public string Notice { get; set; }
        public IdContainer Category { get; set; }
        public IdContainer ContactSource { get; set; }
        public string RegNumber { get; set; }
        public string TaxNumber { get; set; }
        public string TaxNumber2 { get; set; }
        public string TaxPayer { get; set; }
    }
}


