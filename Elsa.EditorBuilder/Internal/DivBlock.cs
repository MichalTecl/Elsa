using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.EditorBuilder.Internal
{
    internal class DivBlock<T> : ICanRender, IFormBuilder<T>
    {
        private readonly IFieldFactory<T> m_owner;
        private readonly List<ICanRender> m_children = new List<ICanRender>();

        public DivBlock(IFieldFactory<T> owner)
        {
            m_owner = owner;
        }

        public string Class { get; set; }
        
        
        public void Render(StringBuilder target)
        {
            target.Append("<div class=\"").Append(Class).Append("\">");
            foreach (var c in m_children)
            {
                c.Render(target);
            }
            target.Append("</div>");
        }

        public IFormBuilder<T> Field<TProperty>(Expression<Func<T, TProperty>> fieldProperty, Action<FormField> setupField = null)
        {
            m_children.Add(m_owner.CreateField(fieldProperty, setupField));

            return this;
        }

        public IFormBuilder<T> Div(string css, Action<IFormBuilder<T>> content)
        {
            var div = new DivBlock<T>(m_owner) { Class = css };
            content(div);
            m_children.Add(div);

            return this;
        }
    }
}
