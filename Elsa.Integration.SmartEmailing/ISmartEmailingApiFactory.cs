using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Integration.SmartEmailing
{
    public interface ISmartEmailingApiFactory
    {
        T Get<T>();
    }
}
