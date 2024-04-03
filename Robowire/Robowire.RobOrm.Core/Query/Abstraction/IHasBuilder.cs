namespace Robowire.RobOrm.Core.Query.Abstraction
{
    public interface IHasBuilder<T> where T : class
    {
        IQueryBuilder<T> OwnerBuilder { get; }
    }
}
