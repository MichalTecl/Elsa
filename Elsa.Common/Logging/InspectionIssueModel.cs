using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common.Logging
{
    public sealed class InspectionIssueModel
    {
        public const string DataEntryMarker = "###DATA_InspectionIssue:";

        public string IssueTypeName { get; set; }
        public string IssueCode { get; set; }
        public string Message { get; set; }
                        
        public static string Serialize(InspectionIssueModel model)
        {
            // serialize provided object to json string:
            using(var writer = new StringWriter())
            {
                writer.Write(DataEntryMarker);
                JsonSerializer.Create().Serialize(writer, model);
                return writer.ToString();
            }            
        }

        public static InspectionIssueModel Deserialize(string serialized)
        {
            //if the input starts with marker, remove it:
            if (serialized.StartsWith(DataEntryMarker))
            {
                serialized = serialized.Substring(DataEntryMarker.Length).Trim();
            }

            // deserialize provided json string to object:
            using (var reader = new StringReader(serialized))
            {
                return JsonSerializer.Create().Deserialize(reader, typeof(InspectionIssueModel)) as InspectionIssueModel;
            }
        }
    }
}
