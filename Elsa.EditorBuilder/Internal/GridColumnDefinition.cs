using System.Text;

namespace Elsa.EditorBuilder.Internal
{
    internal class GridColumnDefinition
    {
        public GridColumnDefinition(string title, string sourceProperty, string cssClass, string controlUrl)
        {
            Title = title;
            SourceProperty = sourceProperty;
            CssClass = cssClass;
            ControlUrl = controlUrl;
        }

        public string Title { get; }

        public string SourceProperty { get; }

        public string CssClass { get; }

        public string ControlUrl { get; }
    
        public virtual void RenderHeader(StringBuilder target)
        {
            target.Append("<div class=\"").Append(CssClass).Append("\">").Append(Title).Append("</div>");
        }

        public virtual void RenderContent(StringBuilder target)
        {
            target.Append("<div class=\"")
                .Append(CssClass)
                .Append("\" ");

            if (!string.IsNullOrWhiteSpace(ControlUrl))
            {
                target.Append(" fill-by=\"").Append(ControlUrl).Append("\"");
            }
            else if (!string.IsNullOrWhiteSpace(SourceProperty))
            {
                target.Append(" data-bind=\"text:").Append(SourceProperty).Append("\" ");
            }
            
            target.Append("\"></div>");
        }
    }
}
