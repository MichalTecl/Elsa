using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.EditorBuilder.Internal
{
    internal interface IFieldFactory<T>
    {
        ICanRender CreateField<TProperty>(Expression<Func<T, TProperty>> fieldProperty, Action<FormField> setupField = null);
    }
}
