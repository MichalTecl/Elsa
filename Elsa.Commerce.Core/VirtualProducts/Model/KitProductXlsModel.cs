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

        [XlsColumn("F", "Název produktu pro zákazníce", "@")]
        public string CustomerProductName { get => _customerProductName?.Trim(); set => _customerProductName = value; }

    }
}
