using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEmailingApi.Client.Messages
{
    public class DataResponse<TData> : SeResponse
    {
        public TData Data { get; set; }
    }
}
