using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Apps.Common;
using Elsa.Apps.CommonData.Model;
using Elsa.Commerce.Core;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.EditorBuilder;
using Elsa.EditorBuilder.Internal;

using Robowire.RoboApi;

namespace Elsa.Apps.CommonData
{
    [Controller("suppliers")]
    public class SuppliersAutoController : AutoControllerBase<SupplierViewModel>
    {
        private readonly ISupplierRepository m_supplierRepository;
        private readonly ICurrencyRepository m_currencyRepository;

        public SuppliersAutoController(IWebSession webSession, ILog log, ISupplierRepository supplierRepository, ICurrencyRepository currencyRepository)
            : base(webSession, log)
        {
            m_supplierRepository = supplierRepository;
            m_currencyRepository = currencyRepository;
        }

        public override EntityListingPage<SupplierViewModel> List(string pageKey)
        {
            return
                new EntityListingPage<SupplierViewModel>(
                    m_supplierRepository.GetSuppliers().OrderBy(s => s.Name).Select(s => new SupplierViewModel(s)));
        }

        public override SupplierViewModel Save(SupplierViewModel entity)
        {
            var currency =
                m_currencyRepository.GetAllCurrencies()
                    .FirstOrDefault(
                        c => c.Symbol.Equals(entity.CurrencyName, StringComparison.InvariantCultureIgnoreCase));

            if (currency == null)
            {
                throw new InvalidOperationException($"Neznámý symbol měny");
            }

            var saved = m_supplierRepository.WriteSupplier(entity.Id,
                s =>
                {
                    s.Name = entity.Name;
                    s.City = entity.City;
                    s.ContactEmail = entity.ContactEmail;
                    s.ContactPhone = entity.ContactPhone;
                    s.Country = entity.Country;
                    s.Street = entity.Street;
                    s.Zip = entity.Zip;
                    s.CurrencyId = currency.Id;
                    s.IdentificationNumber = entity.IdentificationNumber;
                    s.TaxIdentificationNumber = entity.TaxIdentificationNumber;
                    s.Note = entity.Note;
                });

            return new SupplierViewModel(saved);
        }

        public override SupplierViewModel Get(SupplierViewModel uidHolder)
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

        public override SupplierViewModel New()
        {
            return new SupplierViewModel();
        }

        protected override IDefineGrid<SupplierViewModel> SetUidProperty(ISetIdProperty<SupplierViewModel> setter)
        {
            return setter.WithIdProperty(s => s.Id);
        }

        protected override void SetupGrid(GridBuilder<SupplierViewModel> gridBuilder)
        {
            gridBuilder.Column(CellClass.Cell10, s => s.Name)
                .Column(CellClass.Cell10, s => s.ContactPhone)
                .Column(CellClass.Cell10, s => s.ContactEmail);
        }

        protected override void SetupForm(IFormBuilder<SupplierViewModel> formBuilder)
        {
            formBuilder.Div("splHead",
                    f => f.Field(s => s.Name)
                        .Field(s => s.ContactEmail)
                        .Field(s => s.ContactPhone))

                .Div("splIds",
                    f => f.Field(s => s.IdentificationNumber)
                        .Field(s => s.TaxIdentificationNumber))

                .Div("splAddress address",
                    f => f.Field(s => s.Street)
                        .Field(s => s.City)
                        .Field(s => s.Country)
                        .Field(s => s.Zip))
                .Div("splCurr", f => f.Field(s => s.CurrencyName, fld => fld.ReplaceByUrl = "/UI/Controls/Common/Elements/CurrencyDropDown.html"))
                .Div("splNote", f => f.Field(s => s.Note, ff => ff.EditElementNodeType = "textarea"));
        }

        public IEnumerable<string> GetSupplierNames()
        {
            return m_supplierRepository.GetSuppliers().Select(s => s.Name);
        }

        public Dictionary<string, string> GetSupplierCurrencyMap()
        {
            return m_supplierRepository.GetSuppliers().ToDictionary(s => s.Name, s => s.Currency.Symbol);
        }
    }
}
