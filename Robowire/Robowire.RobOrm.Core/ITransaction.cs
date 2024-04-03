using System;

namespace Robowire.RobOrm.Core
{
    public interface ITransaction<TConnection> : ITransaction
    {
        TConnection GetConnection();
    }

    public interface ITransaction : IDisposable
    {
        void Commit();
    }
}
