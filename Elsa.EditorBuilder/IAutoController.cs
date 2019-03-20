using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.EditorBuilder
{
    public interface IAutoController<T> 
    {
        EntityListingPage<T> List(string pageKey);

        IEnumerable<Tuple<string, string>> GetFieldErrors(T entity);

        T Save(T entity);

        T Get(T uidHolder);

        T New();
    }
}
