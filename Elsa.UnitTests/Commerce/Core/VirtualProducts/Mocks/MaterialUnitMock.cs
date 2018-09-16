using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Inventory;

namespace Elsa.UnitTests.Commerce.Core.VirtualProducts.Mocks
{
    internal class MaterialUnitMock : IMaterialUnit
    {
        public static readonly IMaterialUnit Instance = new MaterialUnitMock();

        public int ProjectId { get; set; }

        public IProject Project { get; }

        public int Id => 1;

        public string Symbol
        {
            get
            {
                return "g";
            }
            set { }
        }
    }
}
