using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Apps.Invoices.Model;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.VirtualProducts;

namespace Elsa.Apps.Invoices
{
    public class InvoiceModelFactory
    {
        private readonly ICurrencyRepository m_currencyRepository;
        private readonly IUnitRepository m_unitRepository;
        private readonly ISupplierRepository m_supplierRepository;
        private readonly IMaterialRepository m_materialRepository;

        public InvoiceModelFactory(ICurrencyRepository currencyRepository, IUnitRepository unitRepository,
            ISupplierRepository supplierRepository, IMaterialRepository materialRepository)
        {
            m_currencyRepository = currencyRepository;
            m_unitRepository = unitRepository;
            m_supplierRepository = supplierRepository;
            m_materialRepository = materialRepository;
        }

        public InvoiceModel Create()
        {
            var model = new InvoiceModel();

            model.Currencies.AddRange(m_currencyRepository.GetAllCurrencies().OrderBy(c => c.Symbol).Select(c => c.Symbol));
            model.Units.AddRange(m_unitRepository.GetAllUnits().OrderBy(u => u.Symbol).Select(u => u.Symbol));

            model.Materials.AddRange(m_materialRepository.GetAllMaterials(null).Where(m => !m.IsManufactured).Select(m => new MaterialAndUnit()
            {
                MaterialName = m.Name,
                Unit = m.NominalUnit.Symbol
            }));

            model.Suppliers.AddRange(m_supplierRepository.GetSuppliers().Select(s => new SupplierAndCurrency()
            {
                SupplierName = s.Name,
                Currency = m_currencyRepository.GetCurrency(s.CurrencyId).Symbol
            }));

            model.Date = DateTime.Now.ToString("dd.MM.yyyy");

            for (var i = 0; i < 5; i++)
            {
                model.Items.Add(new InvoiceItem());
            }

            return model;
        }
    }
}
