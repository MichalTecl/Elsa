using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEmailingApi.Client
{
    public interface ISmartEmailingApiFactory
    {
        T Get<T>();
    }
}
