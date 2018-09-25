using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Commerce.Core.VirtualProducts.Model;

using Xunit;

namespace Elsa.UnitTests.Commerce.Core.VirtualProducts
{
    public class MaterialEntryTests
    {
        [Theory]
        [InlineData("123.456", "kg", "mat1")]
        [InlineData("123.456", "kg", "mat mat mat mat")]
        [InlineData("\t\t\t123.456\t", "kg\t\t\t", "mat1\tmat2")]
        public void TestParsing(string amount, string unitName, string materialName)
        {
            var entryText = $"{amount}{unitName} {materialName}";

            var parsed = MaterialEntry.Parse(entryText);

            Assert.Equal(decimal.Parse(amount.Trim()), parsed.Amount);
            Assert.Equal(unitName.Trim(), parsed.UnitName);
            Assert.Equal(materialName.Trim(), parsed.MaterialName);
        }

        [Theory]
        [InlineData("123.456unit")]
        [InlineData("123.456 unit")]
        [InlineData("123.456\tunit")]
        [InlineData(" 123.456unit\t\t")]
        public void TestParsingOfAmountExpression(string expression)
        {
            var parsed = MaterialEntry.Parse(expression, true);

            Assert.NotNull(parsed);
            Assert.Equal(123.456m, parsed.Amount);
            Assert.Equal("unit", parsed.UnitName);
        }


    }
}
