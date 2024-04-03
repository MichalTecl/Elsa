using System;

namespace Robowire.Core
{
    public class CtorParamSetupRecord
    {
        public Type ParameterType { get; set; }

        public string ParameterName { get; set; }

        public NamedFactory ValueProvider { get; set; }
     }
}
