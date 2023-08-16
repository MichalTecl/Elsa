﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Elsa.Integration.Erp.Flox.Protocol.CustomerModel
{
    [XmlRoot(ElementName = "item")]
    public class PersonModel
    {
        [XmlElement("user_id")]
        public string PersonId { get; set; }

        [XmlElement("cid")]
        public string CompanyId { get; set; }

        [XmlElement("email")]
        public string Email { get; set; }

        [XmlElement("name")]
        public string FirstName { get; set; }

        [XmlElement("surname")]
        public string LastName { get; set; }

        [XmlElement("address_phone")]
        public string Phone { get; set; }

        [XmlElement("groups")]
        public string Groups { get; set; }

        [XmlElement("active")]
        public int Active { get; set; }

        [XmlElement("newsletter")]
        public int Newsletter { get; set; }

        [XmlElement("vat_id")]
        public string VatId { get; set; }

        [XmlElement("address_company_name")]
        public string CompanyName { get; set; }

        [XmlElement("address_street")]
        public string Street { get; set; }

        [XmlElement("address_descriptive_number")]
        public string DescriptiveNumber { get; set; }

        [XmlElement("address_orientation_number")]
        public string OrientationNumber { get; set; }

        [XmlElement("address_city")]
        public string City { get; set; }

        [XmlElement("address_zip")]
        public string Zip { get; set; }

        [XmlElement("address_country")]
        public string Country { get; set; }
    }

    [XmlRoot("persons")]
    public class PersonsList
    {
        [XmlElement("item")]
        public List<PersonModel> Items { get; set; }
    }

    [XmlRoot("XML_export")]
    public class PersonsDoc
    {
        [XmlElement("persons")]
        public PersonsList Persons { get; set; }

        public static IEnumerable<PersonModel> Parse(string xml)
        {
            var s = new XmlSerializer(typeof(PersonsDoc));

            using (var textReader = new StringReader(xml))
            {
                var doc = s.Deserialize(textReader) as PersonsDoc;

                if (doc == null)
                {
                    throw new InvalidOperationException("Neocekavany format odpovedi z Floxu");
                }

                return doc.Persons.Items;
            }
        }
    }

}
