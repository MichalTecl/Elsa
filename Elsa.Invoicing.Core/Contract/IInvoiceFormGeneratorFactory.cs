using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Invoicing.Core.Contract
{
    public interface IInvoiceFormGeneratorFactory
    {
        IInvoiceFormGenerator Get(string name);
    }
}
