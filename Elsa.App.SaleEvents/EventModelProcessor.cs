using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.App.SaleEvents.Model.Xls;
using Elsa.Apps.CommonData.ExcelInterop;
using Elsa.Commerce.Core;
using Elsa.Commerce.Core.SaleEvents;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Common;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce.SaleEvents;

namespace Elsa.App.SaleEvents
{
    public class EventModelProcessor
    {
        private readonly ElsaExcelModelFactory m_excelModelFactory;
        private readonly ISaleEventRepository m_saleEventRepository;
        private readonly IMaterialRepository m_materialRepository;
        private readonly IUnitRepository m_unitRepository;

        public EventModelProcessor(ElsaExcelModelFactory excelModelFactory, ISaleEventRepository saleEventRepository, IMaterialRepository materialRepository, IUnitRepository unitRepository)
        {
            m_excelModelFactory = excelModelFactory;
            m_saleEventRepository = saleEventRepository;
            m_materialRepository = materialRepository;
            m_unitRepository = unitRepository;
        }

        public SaleEventModel GetSaleEventModelTemplate()
        {
            var model = m_excelModelFactory.Setup(new SaleEventModel(), m => m.CanBeConnectedToTag, true, false);

            model.AllocDate = DateTime.Now.ToString(ElsaExcelModelBase.ExcelDateFormat);

            for (var i = 0; i < 5; i++)
            {
                model.Items.Add(new SaleEventAllocationModel());
            }

            return model;
        }

        public SaleEventModel ExportEvent(int eventId)
        {
            var saleEvent = m_saleEventRepository.GetEventById(eventId).Ensure();
            
            var model = GetSaleEventModelTemplate();

            model.Id = saleEvent.Id;
            model.Name = saleEvent.Name;

            if (saleEvent.Allocations.Any())
            {
                model.AllocDate = saleEvent.Allocations.Min(a => a.AllocationDt)
                    .ToString(ElsaExcelModelBase.ExcelDateFormat);
            }

            var allocations = saleEvent.Allocations.ToList();

            if (allocations.Any(a => a.ReturnDt != null))
            {
                model.ReturnDate = allocations.Where(a => a.ReturnDt != null).Min(e => e.ReturnDt.Value).ToString(ElsaExcelModelBase.ExcelDateFormat);
            }
            
            model.Items.Clear();

            var itemIndex = new Dictionary<string, SaleEventAllocationModel>();
            
            foreach (var alo in saleEvent.Allocations.OrderBy(a => a.Id))
            {
                var key = $"{alo.Batch.MaterialId}:{alo.Batch.BatchNumber}";

                if (itemIndex.TryGetValue(key, out var item))
                {
                    item.AllocatedQuantity += alo.AllocatedQuantity;

                    if (alo.ReturnedQuantity != null)
                    {
                        item.ReturnQuantity = (item.ReturnQuantity ?? 0m) + alo.ReturnedQuantity.Value;
                    }

                    continue;
                }

                item = new SaleEventAllocationModel
                {
                    AllocatedQuantity = alo.AllocatedQuantity,
                    AllocationUnitSymbol = alo.Unit.Symbol,
                    BatchNumber = alo.Batch.BatchNumber,
                    MaterialName = alo.Batch.Material.Name,
                    ReturnQuantity = alo.ReturnedQuantity
                };

                itemIndex.Add(key, item);

                model.Items.Add(item);
            }

            return model;
        }


        public ISaleEvent Import(SaleEventModel deserializedModel)
        {
            if (string.IsNullOrWhiteSpace(deserializedModel.Name))
            {
                throw new InvalidOperationException("Chybi nazev prodejni akce");
            }

            var alocDate = DateTime.ParseExact(deserializedModel.AllocDate, ElsaExcelModelBase.ExcelDateFormat,
                CultureInfo.InvariantCulture);

            var dtos = new List<SaleEventAllocationDto>(deserializedModel.Items.Count);

            foreach (var item in deserializedModel.Items)
            {
                var material = m_materialRepository.GetMaterialByName(item.MaterialName)
                    .Ensure($"Neznamy material \"{item.MaterialName}");

                var unit = m_unitRepository.GetUnitBySymbol(item.AllocationUnitSymbol)
                    .Ensure($"Neznama merna jednotka '{item.AllocationUnitSymbol}'");

                m_materialRepository.EnsureCompatibleUnit(material, unit);

                Amount returnedQuantity = null;

                if (item.ReturnQuantity != null)
                {
                    returnedQuantity = new Amount(item.ReturnQuantity.Value, unit);
                }

                var dto = new SaleEventAllocationDto(material.Id, item.BatchNumber, new Amount(item.AllocatedQuantity, unit),  returnedQuantity);
                dtos.Add(dto);
            }

            return m_saleEventRepository.WriteEvent(deserializedModel.Id, e =>
            {
                e.Name = deserializedModel.Name;
            }, dtos);
        }
    }
}
