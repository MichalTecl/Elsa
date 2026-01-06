using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Integration.SmartEmailing.Messages;
using Mtecl.ApiClientBuilder;

namespace Elsa.Integration.SmartEmailing
{
    public interface IOrdersImport
    {
        //[Post()]
        Task<SeResponse> ImportOrders(ImportOrdersRequest request);
    }
}
