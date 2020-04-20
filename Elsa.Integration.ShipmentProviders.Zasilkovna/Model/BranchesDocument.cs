using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Elsa.Integration.ShipmentProviders.Zasilkovna.Model
{
    [XmlRoot("export", Namespace = "http://www.zasilkovna.cz/api/v3/branch")]
    public class BranchesDocument
    {
        //private static readonly Dictionary<string, string> s_poboNameMapping = new Dictionary<string, string>();

        //static BranchesDocument()
        //{
        //    s_poboNameMapping.Add("Česká pošta - BALÍK DO RUKY", "Česká pošta");
        //    s_poboNameMapping.Add("Česká republika - KURÝR (DPD)", "Česká republika DPD");
        //    s_poboNameMapping.Add("SLOVENSKO - Kurýr", "Slovensko GLS");
        //    s_poboNameMapping.Add("SLOVENSKO - Slovenská pošta", "Slovenská pošta");
        //}

        [XmlElement("branches")]
        public BranchesList BranchesList { get; set; }

        public string GetPobockaId(string deliveryName, IDictionary<string, string> shipmentMethodsMapping)
        {
            string mapped;
            if (!shipmentMethodsMapping.TryGetValue(deliveryName, out mapped))
            {
                mapped = deliveryName;
            }
            //if (!s_poboNameMapping.TryGetValue(deliveryName, out mapped))
            //{
            //    mapped = deliveryName;
            //}

            var record = BranchesList.Branches.FirstOrDefault(i => i.Name.Equals(mapped, StringComparison.InvariantCultureIgnoreCase))
                      ?? BranchesList.Branches.FirstOrDefault(i => i.LabelName.Equals(mapped, StringComparison.InvariantCultureIgnoreCase));

            if (record == null)
            {
                throw new Exception(string.Format("Neexistující způsob dopravy \"{0}\"", mapped));
            }

            return record.Id;
        }

        public static BranchesDocument Download(string apiToken)
        {
            var request = WebRequest.CreateHttp($"http://www.zasilkovna.cz/api/v3/{apiToken}/branch.xml?type=address-delivery");
            using (var stream = request.GetResponse().GetResponseStream())
            {
                using (var reader = new StreamReader(stream, Encoding.UTF8, true))
                {
                    var deserializer = new XmlSerializer(typeof(BranchesDocument));
                    return deserializer.Deserialize(reader) as BranchesDocument;
                }
            }
        }
    }

    public class BranchesList
    {
        [XmlElement("branch")]
        public List<Branch> Branches { get; set; }
    }

    public class Branch
    {
        [XmlElement("id")]
        public string Id { get; set; }

        [XmlElement("name")]
        public string Name { get; set; }

        [XmlElement("labelName")]
        public string LabelName { get; set; }
    }
}
