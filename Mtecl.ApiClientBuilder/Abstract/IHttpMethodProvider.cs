using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Mtecl.ApiClientBuilder.Abstract
{
    public interface IHttpMethodProvider
    {
        HttpMethod HttpMethod { get; }
    }
}
