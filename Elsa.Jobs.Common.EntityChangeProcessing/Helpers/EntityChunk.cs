using System;
using System.Collections.Generic;

namespace Elsa.Jobs.Common.EntityChangeProcessing.Helpers
{
    public class EntityChunk<T>
    {
        public List<T> Data { get; }

        public bool IsLastPage { get; }

        public object CustomData { get; set; }

        public EntityChunk(List<T> data, bool isLastPage)
        {
            Data = data;
            IsLastPage = isLastPage;
        }
    }
}
