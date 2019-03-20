using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Apps.Suppliers.Model;
using Elsa.Commerce.Core;
using Elsa.Common;
using Elsa.Common.Logging;
using Elsa.EditorBuilder;

using Robowire.RoboApi;

namespace Elsa.Apps.Suppliers
{
    [Controller("suppliers")]
    public class SuppliersAutoController : ElsaControllerBase, IAutoController<SupplierViewModel>
    {
        private readonly ISupplierRepository m_supplierRepository;

        public SuppliersAutoController(IWebSession webSession, ILog log, ISupplierRepository supplierRepository)
            : base(webSession, log)
        {
            m_supplierRepository = supplierRepository;
        }

        [RawString]
        public string GetEditor()
        {
            return GuiFor<SupplierViewModel>
                .Calls<SuppliersAutoController>()
                .WithIdProperty(s => s.Id)
                .ShowsDataInGrid(g => g.Column(CellClass.Cell10, s => s.Name)
                                       .Column(CellClass.Cell5, s => s.ContactPhone)
                                       .Column(CellClass.Cell5, s => s.ContactEmail))
                .ProvidesEditForm(d => d.Div("splHead", f => f.Field(s => s.ContactEmail)
                                                              .Field(s => s.ContactPhone))

                                        .Div("splIds", f => f.Field(s => s.IdentificationNumber)
                                                             .Field(s => s.TaxIdentificationNumber))

                                        .Div("splAddress address", f => f.Field(s => s.Street)
                                                                 .Field(s => s.City)
                                                                 .Field(s => s.Country)
                                                                 .Field(s => s.Zip))
                                        .Div("splCurr", f => f.Field(s => s.CurrencyName))
                                        .Div("splNote", f => f.Field(s => s.CurrencyName, ff => ff.EditElementNodeType = "textarea"))
                                        ).Render();
        }

        public EntityListingPage<SupplierViewModel> List(string pageKey)
        {
            return
                new EntityListingPage<SupplierViewModel>(
                    m_supplierRepository.GetSuppliers().OrderBy(s => s.Name).Select(s => new SupplierViewModel(s)));
        }

        public IEnumerable<Tuple<string, string>> GetFieldErrors(SupplierViewModel entity)
        {
            yield break;
        }

        public SupplierViewModel Save(SupplierViewModel entity)
        {
            throw new NotImplementedException();
        }

        public SupplierViewModel Get(SupplierViewModel uidHolder)
        {
            if (uidHolder.Id == null)
            {
                throw new ArgumentException("id == null");
            }

            var entity = m_supplierRepository.GetSupplier(uidHolder.Id.Value);
            if (entity == null)
            {
                throw new InvalidOperationException("Invalid entity reference");
            }

            return new SupplierViewModel(entity);
        }

        public SupplierViewModel New()
        {
            return new SupplierViewModel();
        }
    }
}
