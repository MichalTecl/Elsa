using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mtecl.ApiClientBuilder.Abstract
{
    public interface IActionUrlProvider
    {
        string Url { get; }
    }
}
