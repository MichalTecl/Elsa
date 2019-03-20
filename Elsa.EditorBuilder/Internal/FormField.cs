using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.EditorBuilder.Internal
{
    public class FormField : ICanRender
    {
        public string Title { get; set; }

        public string EditorElementId { get; set; } = $"f{Guid.NewGuid():N}";

        public string EditElementNodeType { get; set; }

        public string EditElementBoundProperty { get; set; }

        public string ModelBoundProperty { get; set; }

        public string ReplaceByUrl { get; set; }
        
        public HashSet<string> ContainerClasses { get; } = new HashSet<string>() { "autoFormFieldContainer" };

        public HashSet<string> LabelClasses { get; } = new HashSet<string>();

        public HashSet<string> EditorClasses { get; } = new HashSet<string>() { "autoFormEditElement" };

        public IDictionary<string, string> EditorProperties { get; } = new Dictionary<string, string>();

        public void Render(StringBuilder sb)
        {
            sb.Append("<div class=\"").Append(string.Join(" ", ContainerClasses)).Append("\">");

            sb.Append("<label for=\"").Append(EditorElementId).Append("\" class=\"").Append(string.Join(" ", LabelClasses)).Append("\">").Append(Title).Append("<)/label>");

            var node = EditElementNodeType ?? "div";

            sb.Append("<").Append(node).Append(" id=\"").Append(EditorElementId).Append("\" class=\"").Append(string.Join(" ", EditorClasses)).Append("\" ");

            sb.Append("data-bind=\"")
                .Append(EditElementBoundProperty)
                .Append(":editItem.")
                .Append(ModelBoundProperty)
                .Append("\" event-bind=\"change:doValidation\" update-model=\"").Append(ModelBoundProperty).Append(":").Append(EditElementBoundProperty).Append("\" ");

            foreach (var prop in EditorProperties)
            {
                sb.Append(prop.Key).Append("=\"").Append(prop.Value).Append("\" ");
            }

            if (!string.IsNullOrWhiteSpace(ReplaceByUrl))
            {
                sb.Append(" replace-by=\"").Append(ReplaceByUrl).Append("\" ");
            }

            sb.Append("></").Append(node).Append(">");

            sb.Append("</div>");
        }

        public FormField Clone()
        {
            var cl = new FormField()
            {
                EditElementBoundProperty = EditElementBoundProperty,
                EditElementNodeType = EditElementNodeType
            };

            foreach (var property in EditorProperties)
            {
                cl.EditorProperties[property.Key] = property.Value;
            }

            foreach (var containerClass in ContainerClasses)
            {
                cl.ContainerClasses.Add(containerClass);
            }

            foreach (var editorClass in EditorClasses)
            {
                cl.EditorClasses.Add(editorClass);
            }

            foreach (var labelClass in LabelClasses)
            {
                cl.LabelClasses.Add(labelClass);
            }

            return cl;
        }
    }
}
