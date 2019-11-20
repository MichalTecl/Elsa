﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Elsa.Commerce.Core.Units;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Inventory;

using Newtonsoft.Json;

namespace Elsa.Commerce.Core.VirtualProducts.Model
{
    internal class ExtendedMaterial : IExtendedMaterialModel
    {
        private readonly List<MaterialComponent> m_components = new List<MaterialComponent>();

        public ExtendedMaterial(IMaterial adaptee)
        {
            Adaptee = adaptee;
            Id = adaptee.Id;
            Name = adaptee.Name;
            BatchUnit = NominalUnit = adaptee.NominalUnit;
            BatchAmount = NominalAmount = adaptee.NominalAmount;
            InventoryId = adaptee.InventoryId;
            InventoryName = adaptee.Inventory.Name;
            IsManufactured = adaptee.Inventory.IsManufactured;
            CanBeConnectedToTag = adaptee.Inventory.CanBeConnectedToTag;
            AutomaticBatches = adaptee.AutomaticBatches;
            RequiresInvoice = adaptee.RequiresInvoiceNr ?? false;
            RequiresPrice = adaptee.RequiresPrice ?? false;
            RequiresSupplierReference = adaptee.RequiresSupplierReference ?? false;

            var threshold = adaptee.Thresholds?.FirstOrDefault();
            if (threshold != null)
            {
                HasThreshold = true;
                ThresholdText = $"{StringUtil.FormatDecimal(threshold.ThresholdQuantity)} {threshold.Unit.Symbol}";
            }
        }

        public int Id { get; }

        public string Name { get; }

        public IMaterialUnit NominalUnit { get; }

        public decimal NominalAmount { get; }

        public IMaterialUnit BatchUnit { get; private set; }

        public decimal BatchAmount { get; private set; }

        [JsonIgnore]
        public IMaterial Adaptee { get; }
        
        public bool HasThreshold { get; set; }

        public string ThresholdText { get; set; }
        
        public IExtendedMaterialModel CreateBatch(decimal batchAmount, IMaterialUnit preferredBatchUnit, IUnitConversionHelper conversions)
        {
            // Nominal = 1kg
            // Batch = 500g
            var batchUnit = conversions.GetPrefferedUnit(preferredBatchUnit, NominalUnit); //g
            var convertedNominalAmount = conversions.ConvertAmount(NominalUnit.Id, batchUnit.Id, NominalAmount); // 1000g
            var convertedBatchAmount = conversions.ConvertAmount(preferredBatchUnit.Id, batchUnit.Id, batchAmount); //500g

            var conversionFactor = convertedBatchAmount / convertedNominalAmount; // 0.5

            var batch = new ExtendedMaterial(Adaptee) { BatchUnit = batchUnit, BatchAmount = convertedBatchAmount };

            var batchComponents = new List<MaterialComponent>(m_components.Count);

            foreach (var sourceComponent in m_components)
            {
                var componentBatchAmount = sourceComponent.Amount * conversionFactor;

                var batchedComponentMaterial = sourceComponent.Material.CreateBatch(
                    componentBatchAmount,
                    sourceComponent.Unit,
                    conversions);

                var batchComponent = new MaterialComponent(sourceComponent.Unit, batchedComponentMaterial, componentBatchAmount, null);
                batchComponents.Add(batchComponent);
            }
            
            return batch;
        }

        public void Print(StringBuilder target, string depthLevelTrim)
        {
            target.Append(Name);
        }

        public int InventoryId { get; }

        public string InventoryName { get; }

        public bool IsManufactured { get; }

        public bool CanBeConnectedToTag { get; }

        public bool AutomaticBatches { get; }

        public bool RequiresPrice { get; }

        public bool RequiresInvoice { get; }

        public bool RequiresSupplierReference { get; }

    }
}
