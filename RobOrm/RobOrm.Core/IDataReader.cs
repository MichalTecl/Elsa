using System;

namespace RobOrm.Core
{
    public interface IDataReader : IDataRecord, IDisposable
    {
        bool Read();
    }
}
