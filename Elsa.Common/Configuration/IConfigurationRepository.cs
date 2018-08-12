namespace Elsa.Common.Configuration
{
    public interface IConfigurationRepository
    {
        string GetJsonValue(int? projectId, int? userId, string key);

        void SetJsonValue(int? projectId, int? userId, string key, string value);

        T Load<T>(int? projectId, int? userId) where T : new();

        void Save<T>(int? projectId, int? userId, T configSet) where T : new();
    }
}
