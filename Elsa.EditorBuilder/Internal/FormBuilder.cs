using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Elsa.EditorBuilder.Internal
{
    internal class FormBuilder<T> : IFormBuilder<T>, IFieldFactory<T>, ICanRender
    {
        private readonly List<ICanRender> m_fields = new List<ICanRender>();

        public IFormBuilder<T> Field<TProperty>(Expression<Func<T, TProperty>> fieldProperty, Action<FormField> setupField = null)
        {
            m_fields.Add(CreateField(fieldProperty, setupField));

            return this;
        }

        public IFormBuilder<T> Div(string css, Action<IFormBuilder<T>> content)
        {
            var div = new DivBlock<T>(this) { Class = css };
            content(div);
            m_fields.Add(div);

            return this;
        }

        public void Render(StringBuilder formSb)
        {
            foreach (var formField in m_fields)
            {
                formField.Render(formSb);
            }
        }

        public ICanRender CreateField<TProperty>(Expression<Func<T, TProperty>> fieldProperty, Action<FormField> setupField = null)
        {
            var field = DefaultEditControls.GetField(typeof(TProperty));
            var property = ReflectionHelper.GetPropertyInfo(fieldProperty);

            field.ModelBoundProperty = property.Name;

            field.Title = (property.GetCustomAttribute(typeof(DisplayNameAttribute)) as DisplayNameAttribute)?.DisplayName ?? property.Name;

            if (!property.CanWrite)
            {
                field.EditorProperties["readonly"] = "readonly";
            }

            if (property.GetCustomAttribute(typeof(RequiredAttribute)) != null)
            {
                field.EditorProperties["required"] = "required";
            }

            int? minLen = property.GetCustomAttribute<MinLengthAttribute>()?.Length ?? property.GetCustomAttribute<StringLengthAttribute>()?.MinimumLength;
            int? maxLen = property.GetCustomAttribute<MaxLengthAttribute>()?.Length ?? property.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength;

            if (minLen != null)
            {
                field.EditorProperties["minlength"] = minLen.ToString();
            }

            if (maxLen != null)
            {
                field.EditorProperties["maxlength"] = maxLen.ToString();
            }

            var rangeMax = property.GetCustomAttribute<RangeAttribute>()?.Maximum?.ToString();
            var rangeMin = property.GetCustomAttribute<RangeAttribute>()?.Minimum?.ToString();

            if (!string.IsNullOrWhiteSpace(rangeMin))
            {
                field.EditorProperties["min"] = rangeMin;
            }

            if (!string.IsNullOrWhiteSpace(rangeMax))
            {
                field.EditorProperties["max"] = rangeMax;
            }

            setupField?.Invoke(field);

            return field;
        }
    }
}
