using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Common.Security;

namespace Elsa.Core.Entities.Commerce.Core
{
    public interface IChangeControlEntity
    {
        DateTime InsertDt { get; set; }
        int InsertUserId { get; set; }
        IUser InsertUser { get; }

        DateTime? DeleteDt { get; set; }
        int? DeleteUserId { get; set; } 
        IUser DeleteUser { get; }
    }
}
