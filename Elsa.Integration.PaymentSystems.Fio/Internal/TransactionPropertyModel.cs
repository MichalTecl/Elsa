namespace Elsa.Integration.PaymentSystems.Fio.Internal
{
    public class TransactionPropertyModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Value { get; set; }

        public override string ToString()
        {
            return $"{Id}:{Value}";
        }
    }
}
