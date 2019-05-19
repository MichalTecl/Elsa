using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Invoicing.Generation
{
    public interface IContextGenerator
    {
        bool FillNextContext(GenerationContext context);
    }
}
