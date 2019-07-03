using System.Collections.Generic;
using System.Text;

namespace Elsa.Commerce.Core.Model.BatchPriceExpl
{
    public interface IBatchPrice
    {
        bool HasWarning { get; }

        IEnumerable<BatchPriceComponent> Components { get; }

        decimal TotalPriceInPrimaryCurrency { get; }

        string Text { get; }

        void RenderText(StringBuilder target, int level = 0);
    }
}
