using System;

using XlsSerializer.Core.Attributes;

namespace Elsa.Commerce.Core.VirtualProducts.Model
{
    [HeaderStyle(FontStyle = FontStyle.Bold)]
    public class KitProductXlsModel
    {
        private string _kitName;
        private string _selectionGroupName;
        private string _productName;
        private string _shortcut;
        private string _customerGroupTitle;
        private string _customerProductName;
        private string _showPackingCheckboxes;

        [XlsColumn("A", "Sada", "@")]
        public string KitName { get => _kitName?.Trim(); set => _kitName = value; }

        [XlsColumn("B", "Skupina", "@")]
        public string SelectionGroupName { get => _selectionGroupName?.Trim(); set => _selectionGroupName = value; }

        [XlsColumn("C", "Produkt v E-Shopu", "@")]
        public string ProductName { get => _productName?.Trim(); set => _productName = value; }

        [XlsColumn("D", "Zkratka pro balení ", "@")]
        public string Shortcut { get => _shortcut?.Trim(); set => _shortcut = value; }

        [XlsColumn("E", "Název skupiny pro zákaznice", "@")]
        public string CustomerGroupTitle { get => _customerGroupTitle?.Trim(); set => _customerGroupTitle = value; }

        [XlsColumn("F", "Název produktu pro zákaznice", "@")]
        public string CustomerProductName { get => _customerProductName?.Trim(); set => _customerProductName = value; }

        /// <summary>
        /// Platí pro celou sadu; při importu se bere první vyplněná hodnota (Ano/Ne) ze všech řádků sady.
        /// </summary>
        [XlsColumn("G", "Zaškrtávátka při balení (Ano/Ne)", "@")]
        public string ShowPackingCheckboxes { get => _showPackingCheckboxes?.Trim(); set => _showPackingCheckboxes = value; }

        public static string FormatBool(bool? value)
        {
            if (value == null)
                return null;

            return value.Value ? "Ano" : "Ne";
        }

        public static bool? ParseBool(string value)
        {
            value = value?.Trim().ToLowerInvariant();

            if (string.IsNullOrEmpty(value))
                return null;

            switch (value)
            {
                case "ano":
                case "a":
                case "1":
                case "x":
                case "true":
                    return true;
                case "ne":
                case "n":
                case "0":
                case "false":
                    return false;
                default:
                    throw new ArgumentException($"Neplatná hodnota \"{value}\" ve sloupci \"Zaškrtávátka při balení\" - povolené hodnoty jsou Ano, Ne nebo prázdná buňka");
            }
        }

    }
}
