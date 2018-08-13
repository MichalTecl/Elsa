using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace Elsa.Integration.Erp.Flox.Protocol.OrderModel
{
    [XmlRoot("XML_export")]
    public class ExportDocument
    {
        [XmlElement("orders")]
        public OrdersList Orders { get; set; }

        public static ExportDocument Load(string xmlPath)
        {
            var deserializer = new XmlSerializer(typeof(ExportDocument));

            using (var fstrm = File.OpenRead(xmlPath))
            {
                using (var reader = new StreamReader(fstrm, Encoding.UTF8, true))
                {
                    return deserializer.Deserialize(reader) as ExportDocument;
                }
            }
        }

        public static ExportDocument Parse(string ordersDoc)
        {
            var deserializer = new XmlSerializer(typeof(ExportDocument));

            using (var fstrm = new MemoryStream(Encoding.UTF8.GetBytes(ordersDoc)))
            {
                using (var reader = new StreamReader(fstrm, Encoding.UTF8, true))
                {
                    return deserializer.Deserialize(reader) as ExportDocument;
                }
            }
        }
    }
}
