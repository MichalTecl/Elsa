using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Crm;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Model
{
    public class DistributorDetailViewModel : DistributorModelBase
    {        
        public string Phone { get; set; }
        public bool HasEshop { get; set; }
        public bool HasStore { get; set; }
        public string VatId { get; set; }
        public string RegistrationId { get; set; }        
        public int NotesCount { get; set; }        
    }

    public class DistributorAddressViewModel
    {
        private string _address = null;

        [JsonProperty("isDeleted")]
        public bool IsDeleted { get; set; }

        public string AddressName { get; set; }
        public string Street { get; set; }
        public string DescriptiveNumber {  get; set; }
        public string OrientationNumber { get; set; }
        public string Address
        { 
            get => _address ?? (_address = $"{Street} {DescriptiveNumber}/{OrientationNumber}".Trim().TrimEnd('/').Trim());
            set => _address = value;
        }
                
        public string City { get; set; }
        public string Zip { get; set; }
        public string Lat { get; set; }
        public string Lon { get; set; }

        public string Gps 
        { 
            get => $"{Lat}, {Lon}"; 
            set 
            {
                var parts = value?.Split(',').Select(v =>  v.Trim()).Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();
                
                if (parts.Length > 1)
                {
                    Lat = parts.First();
                    Lon = parts.Last();
                }
            }
        }

        public string Phone { get; set; }
        public string Email { get; set; }
        public string Www { get; set; }
        public bool IsStore { get; set; }
        public string StoreName { get; set; }
    }

    public class  CustomerNoteViewModel 
    {
        public string Author { get; set; }
        public string NoteDt { get; set; }
        public string Text { get; set; }
    }
}
