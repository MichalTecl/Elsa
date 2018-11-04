using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Castle.Components.DictionaryAdapter;

using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.UnitTests.Commerce.Core.VirtualProducts.Mocks
{
    public class MaterialEntityMock : IMaterial
    {
        private readonly List<IMaterialComposition> m_composition = new List<IMaterialComposition>();

        public MaterialEntityMock(int id, string name, decimal nominalAmount)
        {
            Id = id;
            Name = name;
            
            NominalAmount = nominalAmount;
        }

        public int ProjectId { get; set; }

        public IProject Project { get; }

        public int Id { get; }

        public string Name { get; set; }

        public int NominalUnitId
        {
            get
            {
                return MaterialUnitMock.Instance.Id;
            }
            set { }
        }

        public IMaterialUnit NominalUnit => MaterialUnitMock.Instance;

        public decimal NominalAmount { get; set; }

        public IEnumerable<IMaterialComposition> Composition => m_composition;

        public IEnumerable<IVirtualProductMaterial> VirtualProductMaterials { get; }

        public int InventoryId { get; set; }

        public IMaterialInventory Inventory { get; }

        public void AddComposition(IMaterial material, decimal amount)
        {
            m_composition.Add(new CompositionMock(this, material, amount));
        }

        private sealed class CompositionMock : IMaterialComposition
        {
            public CompositionMock(IMaterial owner, IMaterial component, decimal amount)
            {
                Composition = owner;
                CompositionId = owner.Id;
                Component = component;
                ComponentId = component.Id;
                Amount = amount;
            }

            public int ComponentId { get; set; }

            public IMaterial Component { get; }

            public int UnitId { get
                {
                    return MaterialUnitMock.Instance.Id;
                }
                set {} }

            public IMaterialUnit Unit => MaterialUnitMock.Instance;

            public decimal Amount { get; set; }

            public int Id { get; }

            public int CompositionId { get; set; }

            public IMaterial Composition { get; }
        }
    }
}
