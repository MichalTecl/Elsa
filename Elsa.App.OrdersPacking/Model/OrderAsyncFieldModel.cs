using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.OrdersPacking.Model
{
    public class OrderAsyncFieldModel<T>
    {
        public long OrderId { get; set; }
        public string FieldName { get; set; }
        public T FieldValue { get; set; }
    }
}
