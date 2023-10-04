using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mtecl.ApiClientBuilder;
using SmartEmailingApi.Client.Messages;

namespace SmartEmailingApi.Client
{
    public interface IOrdersImport
    {
        //[Post()]
        Task<SeResponse> ImportOrders(ImportOrdersRequest request);
    }
}
