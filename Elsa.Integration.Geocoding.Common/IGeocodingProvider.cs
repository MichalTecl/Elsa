using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Common;

namespace Elsa.Integration.Geocoding.Common
{
    public interface IGeocodingProvider
    {
        void SetLatLon(IAddress address);
    }
}
