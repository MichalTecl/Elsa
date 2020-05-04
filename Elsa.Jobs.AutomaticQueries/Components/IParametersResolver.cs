using System;
using System.Collections.Generic;
using System.Text;
using Elsa.Jobs.AutomaticQueries.Database;

namespace Elsa.Jobs.AutomaticQueries.Components
{
    public interface IParametersResolver
    {
        bool TryEvalExpression(string expression, out object result);

        ParametersResolution ResolveParams(IEnumerable<IAutoQueryParameter> parameters, string titlePattern);
    }

    public class ParametersResolution
    {
        public string Trigger { get; set; }

        public string TransformedTitle { get; set; }

        public Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();
    }
}
