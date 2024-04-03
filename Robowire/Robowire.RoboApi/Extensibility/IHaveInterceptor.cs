namespace Robowire.RoboApi.Extensibility
{
    public interface IHaveInterceptor
    {
        IControllerInterceptor Interceptor { get; }
    }
}
