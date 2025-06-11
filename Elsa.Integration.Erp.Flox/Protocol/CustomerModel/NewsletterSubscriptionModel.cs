using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Elsa.Integration.Erp.Flox.Protocol.CustomerModel
{

    /*
     <?xml version="1.0" encoding="utf-8"?>
    <XML_export>
        <newsletterReceivers>
            <item>
                <email>sdfsdf@gmail.com</email>
                <subscribed_when>2018-07-09 13:43:29</subscribed_when>
            </item>
            <item>
                <email>sdfsdf@seznam.cz</email>
                <subscribed_when>2020-06-22 11:22:00</subscribed_when>
            </item>
     */
    [XmlRoot("XML_export")]
    public class NewsletterSubscriptionsModel
    {
        [XmlElement("newsletterReceivers")]
        public Subscriptions Subscriptions { get; set; }

        public static List<NewsletterSubscriptionModel> Parse(string xml)
        {
            var s = new XmlSerializer(typeof(NewsletterSubscriptionsModel));

            using (var textReader = new StringReader(xml))
            {
                var doc = s.Deserialize(textReader) as NewsletterSubscriptionsModel;

                if (doc == null)
                {
                    throw new InvalidOperationException("Neocekavany format odpovedi z Floxu");
                }

                return doc.Subscriptions.Items;
            }
        }
    }

    [XmlRoot("item")]
    public class NewsletterSubscriptionModel
    {
        [XmlElement("email")]
        public string Email { get; set; }

        [XmlElement("subscribed_when")]
        public string SubscriptionDtString { get; set; }

        [XmlElement("valid_consent")]
        public string IsConfirmedSubscriberRaw { get; set; }

        public bool IsConfirmedSubscriber => (!string.IsNullOrWhiteSpace(IsConfirmedSubscriberRaw)) 
                                            && int.TryParse(IsConfirmedSubscriberRaw, out var val) 
                                            && val == 1;

        [XmlIgnore]
        public DateTime SubscriptionDt 
        { 
            get 
            {
                return DateTime.ParseExact(SubscriptionDtString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            }
        }
    }

    public class Subscriptions 
    {
        [XmlElement("item")]
        public List<NewsletterSubscriptionModel> Items { get; set; }
    }
}
