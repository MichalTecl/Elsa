using System;

namespace Robowire.RobOrm.Core
{
    public interface IDataReader : IDataRecord, IDisposable
    {
        bool Read();
    }
}
