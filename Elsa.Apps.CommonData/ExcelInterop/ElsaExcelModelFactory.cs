using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.Apps.CommonData.ExcelInterop
{
    public class ElsaExcelModelFactory
    {
        private readonly IMaterialRepository m_materialRepository;
        private readonly IUnitRepository m_unitRepository;
        private readonly ISupplierRepository m_supplierRepository;
        private readonly ICurrencyRepository m_currencyRepository;

        public ElsaExcelModelFactory(IMaterialRepository materialRepository, IUnitRepository unitRepository, ISupplierRepository supplierRepository, ICurrencyRepository currencyRepository)
        {
            m_materialRepository = materialRepository;
            m_unitRepository = unitRepository;
            m_supplierRepository = supplierRepository;
            m_currencyRepository = currencyRepository;
        }

        public T Setup<T>(T instance, Func<IExtendedMaterialModel, bool> materialFilter, bool loadMaterials = true, bool loadSuppliers = true) where T : ElsaExcelModelBase
        {
            instance.Currencies.AddRange(m_currencyRepository.GetAllCurrencies().Select(c => c.Symbol).OrderBy(c => c));
            instance.Units.AddRange(m_unitRepository.GetAllUnits().Select(u => u.Symbol).OrderBy(u => u));

            if (loadMaterials)
            {
                instance.Materials.AddRange(m_materialRepository.GetAllMaterials(null).Where(materialFilter)
                    .OrderBy(m => m.Name).Select(m => new MaterialAndUnit
                    {
                        MaterialName = m.Name,
                        Unit = m.NominalUnit.Symbol
                    }));
            }

            if (loadSuppliers)
            {
                instance.Suppliers.AddRange(m_supplierRepository.GetSuppliers().OrderBy(s => s.Name).Select(s => new SupplierAndCurrency
                {
                    SupplierName = s.Name,
                    Currency = s.Currency.Symbol
                }));
            }

            return instance;
        }
    }
}
