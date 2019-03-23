using System.Collections.Generic;

using Elsa.EditorBuilder.Internal;

namespace Elsa.EditorBuilder
{
    public interface IAutoController<T> 
    {
        EntityListingPage<T> List(string pageKey);

        IEnumerable<FieldValidationError> GetFieldErrors(T entity);

        T Save(T entity);

        T Get(T uidHolder);

        T New();
    }
}
