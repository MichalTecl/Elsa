namespace Elsa.Common.Communication
{
    public interface IPostBuilder
    {
        IPostBuilder Field(string name, string value);

        string Call();

        T Call<T>();
    }
}
