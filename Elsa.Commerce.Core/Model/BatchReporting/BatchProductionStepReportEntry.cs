using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Commerce.Core.Model.BatchReporting
{
    public class BatchProductionStepReportEntry : BatchReportEntryBase
    {
        public BatchProductionStepReportEntry(int batchId)
            : base(batchId)
        {
        }

        public List<ProductionStepModel> Steps { get; } = new List<ProductionStepModel>();

        #region Nested

        public class ProductionStepModel
        {
            public int MaterialStepId { get; set; }

            public string StepName { get; set; }

            public string DonePercent { get; set; }

            public List<PerformedStepModel> PerformedSteps { get; } = new List<PerformedStepModel>();
        }

        public class PerformedStepModel
        {
            public int StepId { get; set; }

            public string Price { get; set; }

            public string SpentHours { get; set; }

            public string Worker { get; set; }

            public string ConfirmUser { get; set; }

            public string ConfirmDt { get; set; }

            public string Amount { get; set; }

            public bool CanDelete { get; set; }

            public List<PerfomedStepComponent> Components { get; } = new List<PerfomedStepComponent>();
        }

        public class PerfomedStepComponent
        {
            public int StepBatchId { get; set; }

            public string MaterialName { get; set; }

            public string BatchNumber { get; set; }

            public string Amount { get; set; }
        }
        #endregion
    }
}
