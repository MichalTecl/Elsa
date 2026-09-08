using System;

namespace Elsa.Jobs.OrdersPostprocessing.Steps
{
    public interface IOrdersPostprocessStep
    {
        void Process(Action<string> onError);
    }
}
