namespace Robowire.RobOrm.Core.DefaultMethodMappers
{
    public class NotInSubqueryMethodMapper : InSubqueryMethodMapper
    {
        protected override string Operator => " NOT IN ";
    }
}
