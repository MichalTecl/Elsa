using Elsa.Core.Entities.Commerce.Common.Security;
using System;

namespace Elsa.Common.EntityComments
{
    public class EntityComment
    {
        public int Id { get; set; }
        public DateTime PostDt { get; set; }
        public IUser Author { get; set; }
        public int RecordId { get; set; }
        public string Text { get; set; }
    }
}
