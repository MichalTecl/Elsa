namespace Elsa.Integration.Crm.Raynet.Model
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class AddressBucket
    {
        public long? Id { get; set; }

        public Address Address { get; set; }

        public ContactInfo ContactInfo { get; set; }
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
        //public double Lat { get; set; }
        //public double Lng { get; set; }
        public ContactInfo ContactInfo { get; set; }
        //public int Territory { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ContactInfo
    {
        public string Email { get; set; }
        public string Tel1 { get; set; }
        //public string Tel1Type { get; set; }
        //public string Www { get; set; }
        //public bool DoNotSendMM { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Contact
    {
        public long? Id { get; set; }

        public bool Person { get; set; } = false;
        public string Name { get; set; }
        public IdContainer Owner { get; set; }
        public string Rating { get; set; } = "A";
        public string State { get; set; } = "B_ACTUAL";
        public string Role { get; set; } = "A_SUBSCRIBER";
        public string Notice { get; set; }
        public IdContainer Category { get; set; }
        public int? ContactSource { get; set; }
        public int? EmployeesNumber { get; set; }
        public int? LegalForm { get; set; }
        //public string PaymentTerm { get; set; }
        //public int? Turnover { get; set; }
        //public int? EconomyActivity { get; set; }
        //public int? CompanyClassification1 { get; set; }
        //public int? CompanyClassification2 { get; set; }
        //public int? CompanyClassification3 { get; set; }
        public string RegNumber { get; set; }
        public string TaxNumber { get; set; }
        public string TaxPayer { get; set; }
        public string BankAccount { get; set; }
        public List<AddressBucket> Addresses { get; set; }
        //public List<string> Tags { get; set; }
        //public CustomFields CustomFields { get; set; }
    }

}


