using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.EditorBuilder.Internal
{
    internal static class DefaultEditControls
    {
        private static readonly Dictionary<Type, FormField> s_templates = new Dictionary<Type, FormField>();

        private static readonly FormField s_defaultField = new FormField()
        {
            EditElementNodeType = "div",
            EditElementBoundProperty = "text"
        };

        static DefaultEditControls()
        {
            s_templates[typeof(string)] = CreateInput("text");
            s_templates[typeof(int)] = CreateInput("number", i => i.EditorProperties["step"] = "1");
            s_templates[typeof(decimal)] = CreateInput("number", i => i.EditorProperties["step"] = "0.0001");
            s_templates[typeof(DateTime)] = CreateInput("date");
            s_templates[typeof(bool)] = CreateInput("checkbox", i => i.EditElementBoundProperty = "checked");
            s_templates[typeof(int?)] = CreateInput("number", i => i.EditorProperties["step"] = "1");
        }

        public static FormField GetField(Type t)
        {
            FormField template;
            if (!s_templates.TryGetValue(t, out template))
            {
                template = s_defaultField;
            }

            return template.Clone();
        }

        private static FormField CreateInput(string type, Action<FormField> customize = null)
        {
            var input = new FormField()
            {
                EditElementBoundProperty = "value",
                EditElementNodeType = "input"
            };
            input.EditorProperties["type"] = type;

            customize?.Invoke(input);

            return input;
        }
    }
}
