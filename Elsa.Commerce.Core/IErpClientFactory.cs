namespace Elsa.Commerce.Core
{
    public interface IErpClientFactory
    {
        IErpClient GetErpClient(int erpId);
    }
}
