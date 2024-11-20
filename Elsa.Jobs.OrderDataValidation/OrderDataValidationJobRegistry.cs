using Elsa.Jobs.OrderDataValidation.Validations.OrderNote;
using Robowire;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Jobs.OrderDataValidation
{
    public class OrderDataValidationJobRegistry : IRobowireRegistry
    {
        public void Setup(IContainerSetup setup)
        {
            setup.For<KitNoteAiValidator>().Use<KitNoteAiValidator>();
        }
    }
}
