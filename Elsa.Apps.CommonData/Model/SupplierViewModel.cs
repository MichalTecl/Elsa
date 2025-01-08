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
            OrderFulfillDays = src.OrderFulfillDays?.ToString() ?? string.Empty;
        }

        public int? Id { get; set; }
        
        [Display(Name ="Název")]
        [Required]
        public string Name { get; set; }

        [Display(Name ="Ulice a číslo")]
        [Required]
        public string Street { get; set; }

        [Display(Name ="Město")]
        public string City { get; set; }

        [Display(Name ="Stát")]
        public string Country { get; set; }

        [Display(Name ="PSČ")]
        public string Zip { get; set; }

        [Display(Name ="IČO")]
        public string IdentificationNumber { get; set; }

        [Display(Name ="DIČ")]
        public string TaxIdentificationNumber { get; set; }

        [Display(Name ="Kontaktní telefon")]
        public string ContactPhone { get; set; }

        [Display(Name ="Kontaktní e-mail")]
        public string ContactEmail { get; set; }

        [Display(Name ="Měna")]
        public string CurrencyName { get; set; }

        [Display(Name ="Poznámky")]
        public string Note { get; set; }

        [Display(Name = "Dny na objednávku")]
        public string OrderFulfillDays { get; set; }
    }
}
