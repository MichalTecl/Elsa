using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common.Logging.Helpers
{
    internal class PrettifyJsonResponseExtra : IRequestProtocolExtra
    {
        public void Apply(string method, string url, string sent, string received, StringBuilder target)
        {
            try
            {
                string prettifiedJson = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(received), Formatting.Indented);
                target.AppendLine().AppendLine("PRETTIFIED RESPONSE:").AppendLine(prettifiedJson);
            }
            catch (Exception ex)
            {
                target.AppendLine();
                target.AppendLine("PrettifyResponse failed :(");
                target.AppendLine(ex.ToString());
            }
        }
    }
}
