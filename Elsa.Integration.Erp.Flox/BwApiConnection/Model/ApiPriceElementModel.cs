using BwApiClient.Model.Data;
using Elsa.Commerce.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Integration.Erp.Flox.BwApiConnection.Model
{
    public class ApiPriceElementModel : IErpPriceElementModel
    {
        private readonly OrderPriceElement _source;

        public ApiPriceElementModel(OrderPriceElement source)
        {
            _source = source ?? throw new InvalidOperationException("Seznam cenových položek objednávky obsahuje prázdný záznam.");

            if (_source.price == null)
            {
                var identification = _source.id ?? _source.title ?? _source.type ?? "bez ID a názvu";
                throw new InvalidOperationException($"Cenové položce {identification} chybí cena (price).");
            }
        }

        public string ErpPriceElementId => _source.id;

        public string TypeErpName => _source.type;

        public string Title => _source.title;

        public string Value => _source.value;

        public string TaxPercent => _source.tax_rate?.ToString();

        public string Price => _source.price?.value.ToString();
    }
}
