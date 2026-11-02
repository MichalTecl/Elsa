using System.Configuration;

using RobOrm.SqlServer;

namespace Elsa.Core.Entities.Commerce
{
    public class ConnectionStringProvider : ISqlConnectionStringProvider
    {
        public string ConnectionString => ConfigurationManager.ConnectionStrings["ElsaDb"].ConnectionString;
    }
}
