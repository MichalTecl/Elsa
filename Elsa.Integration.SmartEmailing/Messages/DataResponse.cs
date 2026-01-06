using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Integration.SmartEmailing.Messages
{
    public class DataResponse<TData> : SeResponse
    {
        public TData Data { get; set; }
    }
}
