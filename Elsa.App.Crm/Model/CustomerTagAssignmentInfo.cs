using Elsa.Common.Utils;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Model
{
    public class CustomerTagTypeInfo
    {        
        public int TagTypeId { get; set; }
        public string TagTypeName { get; set; }
        public string TagTypeCssClass { get; set; }
        public int? DaysToWarning { get; set; }
        public bool HasTransitions { get; set; }
        public int TagTypeGroupId { get; set; }
        public bool RequiresNote { get; set; }
    }

    public class CustomerTagAssignmentInfo : CustomerTagTypeInfo
    {
        public int CustomerId { get; set; }
        public string AssignedBy { get; set; }
        public DateTime AssignDt { get; set; }                
        public string Assigned => StringUtil.FormatDate(AssignDt);
        public int? TimeoutPercent 
        {
            get
            {
                if ((DaysToWarning ?? 0) < 1)
                    return null;

                var spendDays = (DateTime.Now - AssignDt).TotalDays;
                var totalDays = (double)DaysToWarning.Value;

                return (int)Math.Round(spendDays / totalDays * 100, 0);
            }
        }
        public bool HasTimeoutWarning 
        {
            get 
            {
                if ((DaysToWarning ?? 0) < 1)
                    return false;

                return (AssignDt.AddDays(DaysToWarning.Value) > DateTime.Now);
            } 
        }        

        public string Note { get; set; }
    }

    public class TagTransitionInfo : CustomerTagTypeInfo
    {
        public int CustomerId { get; set; }
        public int FromTagTypeId { get; set; }
    }
}
