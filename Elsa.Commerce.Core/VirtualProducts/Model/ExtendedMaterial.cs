using System.Collections.Generic;
using System.Linq;
using System.Text;

using Elsa.Commerce.Core.Units;
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
        }

        public int Id { get; }

        public string Name { get; }

        public IMaterialUnit NominalUnit { get; }

        public decimal NominalAmount { get; }

        public IMaterialUnit BatchUnit { get; private set; }

        public decimal BatchAmount { get; private set; }

        [JsonIgnore]
        public IMaterial Adaptee { get; }
        
        public IEnumerable<MaterialComponent> Components => m_components;

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

            batch.AdoptComponents(batchComponents);

            return batch;
        }

        public IEnumerable<CompositionViewModel> Flatten()
        {
            var result = new List<CompositionViewModel>();
            Flatten(0, this, result);

            return result;
        }

        public void Print(StringBuilder target, string depthLevelTrim)
        {
            foreach (var f in Flatten())
            {
                f.Print(target, depthLevelTrim);
            }
        }

        public void AddComponent(decimal amount, IMaterialUnit unit, IExtendedMaterialModel material)
        {
            m_components.Add(new MaterialComponent(unit, material, amount, null));
        }

        private void AdoptComponents(IEnumerable<MaterialComponent> components)
        {
            m_components.Clear();
            m_components.AddRange(components);
        }

        private static void Flatten(int depth, IExtendedMaterialModel model, IList<CompositionViewModel> target)
        {
            target.Add(new CompositionViewModel(model, model.BatchAmount, model.BatchUnit, depth));

            foreach (var c in model.Components)
            {
                Flatten(depth+1, c.Material, target);
            }
        }
    }
}
