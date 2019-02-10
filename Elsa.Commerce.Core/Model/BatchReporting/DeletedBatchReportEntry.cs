namespace Elsa.Commerce.Core.Model.BatchReporting
{
    public class DeletedBatchReportEntry : BatchReportEntryBase
    {
        public DeletedBatchReportEntry(int batchId)
            : base(batchId)
        {
        }

        public bool IsDeleted => true;
    }
}
