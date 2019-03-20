using System;
using System.Linq.Expressions;

namespace Elsa.EditorBuilder.Internal
{
    public interface IFormBuilder<T> 
    {
        IFormBuilder<T> Field<TProperty>(Expression<Func<T, TProperty>> fieldProperty, Action<FormField> setupField = null);

        IFormBuilder<T> Div(string css, Action<IFormBuilder<T>> content);
    }
}
