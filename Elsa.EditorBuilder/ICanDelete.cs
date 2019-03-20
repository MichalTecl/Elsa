namespace Elsa.EditorBuilder
{
    public interface ICanDelete<T> 
    {
        void Delete(T entity);
    }
}
