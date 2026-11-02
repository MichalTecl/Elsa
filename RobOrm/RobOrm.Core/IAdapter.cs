namespace RobOrm.Core
{
    public interface IAdapter<T>
    {
        T Adaptee { get; }
    }
}
