using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Robowire.RobOrm.SqlServer;

namespace Elsa.Core.Entities.Commerce
{
    public class ConnectionStringProvider : ISqlConnectionStringProvider
    {
        public string ConnectionString => @"Data Source=MTECL-PRG-L\SQL2014;Initial Catalog=test;Integrated Security=True";
    }
}
