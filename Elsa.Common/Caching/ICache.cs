﻿using System;
using System.Collections.Generic;

namespace Elsa.Common.Caching
{
    public interface ICache
    {
        T ReadThrough<T>(string key, TimeSpan timeout, Func<T> factory);

        void Remove(string key);

        IEnumerable<string> GetAllKeys();

        void Clear();
    }
}
