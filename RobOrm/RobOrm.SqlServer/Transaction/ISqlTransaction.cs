using System.Data.SqlClient;

using RobOrm.Core;

namespace RobOrm.SqlServer.Transaction
{
    internal interface ISqlTransaction : ITransaction<SqlConnection>
    {
        ISqlTransaction Parent { get; }

        void Rollback();
    }
}
