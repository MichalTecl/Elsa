using Elsa.Jobs.Common;
using Elsa.Jobs.OrderDataValidation.Validations.OrderNote;
using System;

namespace Elsa.Jobs.OrderDataValidation
{
    public class OrderDataValidationJob : IExecutableJob
    {
        private readonly KitNoteAiValidator _kitNoteAiValidator;

        public OrderDataValidationJob(KitNoteAiValidator kitNoteAiValidator)
        {
            _kitNoteAiValidator = kitNoteAiValidator;
        }

        public void Run(string customDataJson)
        {
            _kitNoteAiValidator.Validate();
        }
    }
}
