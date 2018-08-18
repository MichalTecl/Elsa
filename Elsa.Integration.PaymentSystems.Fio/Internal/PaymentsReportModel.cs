namespace Elsa.Integration.PaymentSystems.Fio.Internal
{
    public class PaymentsReportModel
    {
        public AccountStatementModel AccountStatement { get; set; }

        public int TransactionsCount
        {
            get
            {
                return AccountStatement.TransactionList.Transaction.Count;
            }
        }
    }
}
