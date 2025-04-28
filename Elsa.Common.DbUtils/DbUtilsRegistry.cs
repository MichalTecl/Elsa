using Robowire;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Common.DbUtils
{
    public class DbUtilsRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<IProcedureLister>().Use<ProcedureLister>();
        }
    }
}
