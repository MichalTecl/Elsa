using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using Elsa.Core.Entities.Commerce.Commerce;

namespace Elsa.Apps.CommonData.Model
{
    public class SupplierViewModel 
    {
        public SupplierViewModel() { }

        public SupplierViewModel(ISupplier src)
        {
            Id = src.Id;
            Name = src.Name;
            Street = src.Street;
            City = src.City;
            Country = src.Country;
            Zip = src.Zip;
            IdentificationNumber = src.IdentificationNumber;
            TaxIdentificationNumber = src.TaxIdentificationNumber;
            ContactPhone = src.ContactPhone;
            ContactEmail = src.ContactEmail;
            Note = src.Note;
            CurrencyName = src.Currency.Symbol;
        }

        public int? Id { get; set; }
        
        [DisplayName("Název")]
        [Required]
        public string Name { get; set; }

        [DisplayName("Ulice a číslo")]
        [Required]
        public string Street { get; set; }

        [DisplayName("Město")]
        public string City { get; set; }

        [DisplayName("Stát")]
        public string Country { get; set; }

        [DisplayName("PSČ")]
        public string Zip { get; set; }

        [DisplayName("IČO")]
        public string IdentificationNumber { get; set; }

        [DisplayName("DIČ")]
        public string TaxIdentificationNumber { get; set; }

        [DisplayName("Kontaktní telefon")]
        public string ContactPhone { get; set; }

        [DisplayName("Kontaktní e-mail")]
        public string ContactEmail { get; set; }

        [DisplayName("Měna")]
        public string CurrencyName { get; set; }

        [DisplayName("Poznámky")]
        public string Note { get; set; }
    }
}
