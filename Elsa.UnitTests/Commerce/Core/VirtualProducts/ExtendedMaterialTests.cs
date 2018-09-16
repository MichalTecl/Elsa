using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.Units;
using Elsa.Commerce.Core.VirtualProducts;
using Elsa.Commerce.Core.VirtualProducts.Model;
using Elsa.Core.Entities.Commerce.Inventory;
using Elsa.UnitTests.Commerce.Core.VirtualProducts.Mocks;

using Moq;

using Xunit;

namespace Elsa.UnitTests.Commerce.Core.VirtualProducts
{
    public class ExtendedMaterialTests
    {
        private readonly MaterialEntityMock m_groundCofee = new MaterialEntityMock(1, "Ground Coffee", 1000);
        private readonly MaterialEntityMock m_hotWater = new MaterialEntityMock(2, "Hot Water", 1000);
        private readonly MaterialEntityMock m_milk = new MaterialEntityMock(3, "Milk", 1000);

        private readonly MaterialEntityMock m_espresso;
        private readonly MaterialEntityMock m_doubleEspresso;
        private readonly MaterialEntityMock m_flatWhite;

        private readonly IMaterialRepository m_materialRepository;

        public ExtendedMaterialTests()
        {
            m_espresso = new MaterialEntityMock(4, "Espresso", 100);
            m_espresso.AddComposition(m_groundCofee, 8);
            m_espresso.AddComposition(m_hotWater, 92);

            m_doubleEspresso = new MaterialEntityMock(5, "Double Espresso", 200);
            m_doubleEspresso.AddComposition(m_espresso, 200);

            m_flatWhite = new MaterialEntityMock(6, "Flat White", 400);
            m_flatWhite.AddComposition(m_doubleEspresso, 200);
            m_flatWhite.AddComposition(m_milk, 200);

            m_materialRepository = new MaterialRepositoryMock(
                                       m_groundCofee,
                                       m_hotWater,
                                       m_milk,
                                       m_espresso,
                                       m_doubleEspresso,
                                       m_flatWhite);
        }

        [Fact]
        public void MaterialEntitiesConvertedToExtendedMaterials()
        {
            var flatWhite = m_materialRepository.GetMaterialById(m_flatWhite.Id);

            Assert.NotNull(flatWhite);
            Assert.Equal(2, m_flatWhite.Composition.Count());
        }

        [Fact]
        public void TestBatchCalculation()
        {
            var flatWhite = m_materialRepository.GetMaterialById(m_flatWhite.Id);

            //let's get 200g of Flat White
            var batch = flatWhite.CreateBatch(200, MaterialUnitMock.Instance, new UnitConversionHelperMock());

            Assert.Equal(200m, batch.BatchAmount);

            var doubleEspresso = batch.Components.FirstOrDefault(c => c.Material.Id == m_doubleEspresso.Id)?.Material;
            var milk = batch.Components.FirstOrDefault(c => c.Material.Id == m_milk.Id)?.Material;

            Assert.NotNull(doubleEspresso);
            Assert.NotNull(milk);

            Assert.Equal(200m / 2m, doubleEspresso.BatchAmount);
            Assert.Equal(200m / 2m, milk.BatchAmount);

            Assert.Empty(milk.Components);

            Assert.Single(doubleEspresso.Components);

            var espresso = doubleEspresso.Components.Single(c => c.Material.Id == m_espresso.Id)?.Material;
            
            Assert.Equal(100m, espresso.BatchAmount);

            var groundCoffee = espresso.Components.FirstOrDefault(c => c.Material.Id == m_groundCofee.Id)?.Material;
            var hotWater = espresso.Components.FirstOrDefault(c => c.Material.Id == m_hotWater.Id)?.Material;

            Assert.NotNull(groundCoffee);
            Assert.NotNull(hotWater);

            Assert.Equal(16m / 2m, groundCoffee.BatchAmount);
            Assert.Equal(92m, hotWater.BatchAmount);
        }

        [Fact]
        public void TestFlattening()
        {
            var flatWhiteBatch = m_materialRepository.GetMaterialById(m_flatWhite.Id);

            //let's get 800g of Flat White
            var batch = flatWhiteBatch.CreateBatch(800, MaterialUnitMock.Instance, new UnitConversionHelperMock());

            var flatBatch = batch.Flatten().ToList();

            Assert.Equal(6, flatBatch.Count);

            Func<int, CompositionViewModel> find = id => flatBatch.Single(i => i.Material.Id == id);

            var flatWhite = find(m_flatWhite.Id);
            var espresso = find(m_espresso.Id);
            var doubleEspresso = find(m_doubleEspresso.Id);
            var groundCoffee = find(m_groundCofee.Id);
            var hotWater = find(m_hotWater.Id);
            var milk = find(m_milk.Id);

            Assert.Equal(800m, flatWhite.Amount);
            Assert.Equal(0, flatWhite.Depth);

            Assert.Equal(400m, doubleEspresso.Amount);
            Assert.Equal(1, doubleEspresso.Depth);

            Assert.Equal(400m, milk.Amount);
            Assert.Equal(1, milk.Depth);

            Assert.Equal(400m, espresso.Amount);
            Assert.Equal(2, espresso.Depth);

            Assert.Equal(4m * 8m, groundCoffee.Amount);
            Assert.Equal(3, groundCoffee.Depth);

            Assert.Equal(4m * 92m, hotWater.Amount);
            Assert.Equal(3, hotWater.Depth);

            var sb = new StringBuilder();
            batch.Print(sb, " ");
            var s = sb.ToString();

            Assert.NotEmpty(s);
        }
    }
}

