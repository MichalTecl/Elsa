using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mtecl.ApiClientBuilder.Proxy
{
    internal interface IHasSettings
    {
        void Initialize(ApiClientFactorySettings settings);
    }
}
