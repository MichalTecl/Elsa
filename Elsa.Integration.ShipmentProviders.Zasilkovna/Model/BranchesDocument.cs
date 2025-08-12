using Elsa.Common.Logging;
using Newtonsoft.Json;
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
        [XmlElement("branches")]
        public BranchesList BranchesList { get; set; }
        
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

        public static BranchesDocument DownloadV5(ILog log, string apiToken)
        {
            List<Branch> branches = new List<Branch>(1024);

            //Download(log, "branch", apiToken, branches);
            //Download(log, "box", apiToken, branches);
            Download(log, "carrier", apiToken, branches);

            return new BranchesDocument 
            { 
                BranchesList = new BranchesList 
                {
                    Branches = branches
                }
            };
        }

        private static void Download(ILog log, string typeName, string apiToken, List<Branch> branches)
        {
            var url = $"https://pickup-point.api.packeta.com/v5/{apiToken}/{typeName}/json";

            var logUrl = url.Replace(apiToken, "__apiKey__");

            log.Info($"Downloading {logUrl}");

            try
            {
                var request = WebRequest.CreateHttp(url);
                using (var stream = request.GetResponse().GetResponseStream())
                {
                    using (var reader = new StreamReader(stream, Encoding.UTF8, true))
                    {
                        var json = reader.ReadToEnd();
                        log.SaveRequestProtocol("GET", logUrl, logUrl, json);

                        var content = JsonConvert.DeserializeObject<List<Branch>>(json);

                        log.Info($"Deserialized to {content.Count} items");

                        foreach(var item in content)
                        {
                            var existingItem = branches.FirstOrDefault(ex => ex.Id == item.Id);
                            if (existingItem == null)
                            {
                                branches.Add(item);
                                continue;
                            }

                            if (existingItem.Name != item.Name || existingItem.LabelName != item.LabelName)
                            {
                                log.Error($"There is already a branch with Id={existingItem.Id}. ( {existingItem.Name}; {existingItem.LabelName} ) x ({item.Name}; {item.LabelName})");

                            }
                        }
                    }
                }
            }
            catch (Exception ex) 
            {                
                log.Error($"Failed: {logUrl}", ex);
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
