namespace Elsa.Commerce.Core.Model.BatchReporting
{
    public class DeletedBatchReportEntry : BatchReportEntryBase
    {
        public DeletedBatchReportEntry(BatchKey batchKey)
            : base(batchKey)
        {
        }

        public bool IsDeleted => true;
    }
}
