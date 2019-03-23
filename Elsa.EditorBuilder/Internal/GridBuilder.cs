using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Elsa.EditorBuilder.Internal
{
    public class GridBuilder<T>
    {
        private readonly List<GridColumnDefinition> m_columns = new List<GridColumnDefinition>();

        private readonly string m_keyPropertyName;

        public GridBuilder(string keyPropertyName)
        {
            m_keyPropertyName = keyPropertyName;
        }

        public GridBuilder<T> ControlColumn(string cssClass, string title, string controlUrl)
        {
            m_columns.Add(new GridColumnDefinition(title, null, cssClass, controlUrl));

            return this;
        }

        public GridBuilder<T> Column<TProperty>(string cssClass, Expression<Func<T, TProperty>> property, string title = null)
        {
            var p = ReflectionHelper.GetPropertyInfo(property);

            title = title ?? p.GetCustomAttribute<DisplayAttribute>()?.Name ?? p.Name;

            m_columns.Add(new GridColumnDefinition(title, p.Name, cssClass, null));

            return this;
        }

        public GridBuilder<T> EditActionColumn()
        {
            m_columns.Add(new EditButtonColumnDefinition());

            return this;
        }

        public virtual void Render(StringBuilder stringBuilder)
        {
            stringBuilder.Append("<div class=\"gridRow gridHead autoGridHead\">");

            foreach (var column in m_columns)
            {
                column.RenderHeader(stringBuilder);
            }

            stringBuilder.Append("</div>");

            stringBuilder.Append($"<div class=\"autoGridBody\" data-key=\"{m_keyPropertyName}\" data-bind=\"itemsSource:items\">");

            stringBuilder.Append("<div class=\"lt-template autoGridRow gridRow\">");
            foreach (var column in m_columns)
            {
                column.RenderContent(stringBuilder);
            }

            stringBuilder.Append("</div>");
            stringBuilder.Append("</div>");
        }
    }
}
