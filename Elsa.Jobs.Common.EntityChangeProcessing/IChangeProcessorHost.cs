namespace Elsa.Jobs.Common.EntityChangeProcessing
{
    public interface IChangeProcessorHost<T>
    {
        void Execute(IEntityChangeProcessor<T> processor);
    }
}
