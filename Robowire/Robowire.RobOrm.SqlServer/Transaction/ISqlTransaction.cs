using System.Data.SqlClient;

using Robowire.RobOrm.Core;

namespace Robowire.RobOrm.SqlServer.Transaction
{
    internal interface ISqlTransaction : ITransaction<SqlConnection>
    {
        ISqlTransaction Parent { get; }

        void Rollback();
    }
}
