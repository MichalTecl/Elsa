namespace Robowire.RobOrm.Core
{
    public interface IDataRecord
    {
        string RootPath { get; }

        IDataRecord GetDeeperReader(string pathSegment);

        T Get<T>(string columnName);

        T Get<T>(int columnOrdinal);

        bool IsNull(string columnName);
    }
}
