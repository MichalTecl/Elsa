using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Jobs.OrderDataValidation.Validations.OrderNote.Model
{
    internal class KitNoteValidationHistory
    {
        public long OrderId { get; set; }
        public int Quantity { get; set; }
        public int OrderItemId { get; set; }
        public int KitDefinitionId { get; set; }
        public bool RequiresSelection { get; set; }
        public bool? IsValid { get; set; }
        public DateTime? LastValidationDt { get; set; }
        public string CustomerNoteHash { get; set; }
    }
}
