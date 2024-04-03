namespace Robowire.RobOrm.Core
{
    public interface ITransactionManager<TConnection>
    {
        ITransaction<TConnection> Open(bool childOnly);

        TConnection OpenUnmanagedConnection();
    }
}
