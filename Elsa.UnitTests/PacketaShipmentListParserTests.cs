using System;
using System.Collections.Generic;
using Elsa.Integration.ShipmentProviders.Zasilkovna;
using Xunit;

namespace Elsa.UnitTests
{
    public class PacketaShipmentListParserTests
    {
        [Fact]
        public void MatchesReferenceAndSenderAndUsesPacketaTrackingNumber()
        {
            var index = new Dictionary<string, HashSet<string>>();
            PacketaShipmentListParser.AddRows(Row("123", "00123", "shop&amp;co") + Row("456", "00123", "other"), "shop&co", index);
            Assert.Single(index);
            Assert.Equal("Z123", Assert.Single(index["00123"]));
        }

        [Fact]
        public void RepeatedRowsAreDeduplicatedButDifferentShipmentsArePreserved()
        {
            var index = new Dictionary<string, HashSet<string>>();
            PacketaShipmentListParser.AddRows(Row("123", "order", "shop") + Row("123", "order", "shop") + Row("456", "order", "shop"), "shop", index);
            Assert.Equal(2, index["order"].Count);
        }

        [Fact]
        public void RejectsChangedColumnsAndMismatchedTrackingLink()
        {
            Assert.Throws<InvalidOperationException>(() => PacketaShipmentListParser.AddRows(
                Row("123", "order", "shop").Replace("col-number", "renamed"), "shop", new Dictionary<string, HashSet<string>>()));
            Assert.Throws<InvalidOperationException>(() => PacketaShipmentListParser.AddRows(
                Row("123", "order", "shop").Replace("?id=Z123", "?id=Z456"), "shop", new Dictionary<string, HashSet<string>>()));
        }

        [Fact]
        public void DoesNotTreatLoginPageAsEmptyShipmentList()
        {
            Assert.Throws<InvalidOperationException>(() => PacketaShipmentListParser.AddRows(
                "<form id='frm-signInForm'><input name='email'></form>", "shop", new Dictionary<string, HashSet<string>>()));
        }

        [Fact]
        public void EmptyBodyProducesEmptyIndex()
        {
            var index = new Dictionary<string, HashSet<string>>();
            PacketaShipmentListParser.AddRows("\r\n ", "shop", index);
            Assert.Empty(index);
        }

        private static string Row(string id, string reference, string sender)
        {
            return $"<tr data-id='{id}'><td class='text-left col-senderId'>{sender}</td>"
                + $"<td class='col-barcode'>Z {id}, CARRIER999</td><td class='text-left col-number'> {reference} </td>"
                + $"<td><a href='https://tracking.packeta.com/cs/?id=Z{id}'>Tracking</a></td></tr>";
        }
    }
}
