using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Invoicing.Core.Contract;

using Robowire;

namespace Elsa.Invoicing.Generation
{
    public class Registry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            var jst = new JobsSetup();
            setup.For<GenerationJobList>().Import.FromFactory(l => new GenerationJobList(jst.CreateJobs(l)));
        }
    }
}
