using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Jobs.Common.EntityChangeProcessing
{
    public interface IChangeProcessorHostFactory
    {
        IChangeProcessorHost<T> Get<T>();
    }
}
