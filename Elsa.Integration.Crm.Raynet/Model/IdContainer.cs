using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Integration.Crm.Raynet.Model
{
    public class IdContainer
    {
        public long? Id { get; set; }

        public static IdContainer Get(long? id) 
        {
            if (id == null)
                return null;

            return new IdContainer { Id = id };
        }
    }
}
