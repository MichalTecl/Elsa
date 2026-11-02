using System.Collections.Generic;

namespace RobOrm.Core
{
    public interface IHasParameters
    {
        int ParametersCount { get; }

        void AddParameter(string name, object value);

        IEnumerable<KeyValuePair<string, object>> GetParameters();
    }
}
