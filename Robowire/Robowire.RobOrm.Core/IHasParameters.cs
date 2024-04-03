using System.Collections.Generic;

namespace Robowire.RobOrm.Core
{
    public interface IHasParameters
    {
        int ParametersCount { get; }

        void AddParameter(string name, object value);

        IEnumerable<KeyValuePair<string, object>> GetParameters();
    }
}
