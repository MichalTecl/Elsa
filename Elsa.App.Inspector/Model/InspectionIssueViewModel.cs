using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.App.Inspector.Database;

namespace Elsa.App.Inspector.Model
{
    public class InspectionIssueViewModel
    {
        public InspectionIssueViewModel() { }

        public InspectionIssueViewModel(IInspectionIssue issue) : this()
        {
            IssueId = issue.Id;
            Message = issue.Message;
            ActionControls.AddRange(issue.Actions.Select( a => new IssueActionModel
            {
                Control = a.ControlUrl,
                ActionText = a.ActionName
            }));

            var arrays = issue.Data.Where(d => d.IsArray == true).Select(d => d.PropertyName).Distinct().ToList();

            foreach (var i in issue.Data)
            {
                var theValue = i.StrValue == null ? (object)i.IntValue : (object)i.StrValue;

                if (arrays.Contains(i.PropertyName))
                {
                    if (!Data.TryGetValue(i.PropertyName, out var objArray))
                    {
                        objArray = new List<object>();
                        Data[i.PropertyName] = objArray;
                    }
                    
                    ((List<object>)objArray).Add(theValue);
                }
                else
                {
                    Data[i.PropertyName] = theValue;
                }
            }
        }


        public int IssueId { get; set; }
        public string Message { get; set; }

        public List<IssueActionModel> ActionControls { get; } = new List<IssueActionModel>();

        public Dictionary<string, object> Data { get; } = new Dictionary<string, object>();
        public bool IsHidden { get; set; }
    }

    public class IssueActionModel
    {
        public string Control { get; set; }

        public string ActionText { get; set; }
    }
    
}
