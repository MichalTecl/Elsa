using System;
using System.Data.SqlClient;

namespace Robowire.RobOrm.Core.NonOrm
{
    public interface IExecutor
    {
        T Execute<T>(Action<SqlCommand> setupCommand, Func<SqlCommand, T> action);
    }
}
