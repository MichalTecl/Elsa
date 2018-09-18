using System.Collections.Generic;

using Castle.Components.DictionaryAdapter;

using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.UnitTests.Mocks;

namespace Elsa.UnitTests.Commerce.Core.VirtualProducts.Mocks
{
    public class MaterialRepositoryMock : MaterialRepository
    {
        private readonly List<IMaterial> m_materialEntities;

        public MaterialRepositoryMock(params IMaterial[] materials)
            : base(null, new SessionMock(), new CacheMock(), new UnitConversionHelperMock())
        {
            m_materialEntities = new EditableList<IMaterial>(materials);
        }

        protected override IEnumerable<IMaterial> LoadMaterialEntities()
        {
            return m_materialEntities;
        }
    }
}
