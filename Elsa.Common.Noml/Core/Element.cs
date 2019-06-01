using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;

namespace Elsa.Common.Noml.Core
{
    public class Element : IElement
    {
        public Element(string type, params object[] nodes)
        {
            Type = type;

            ProcessNodes(nodes);
        }

        public bool IsPairTag { get; set; } = true;

        private void ProcessNodes(IEnumerable<object> nodes)
        {
            foreach (var node in nodes)
            {
                this.NewNode(node);
            }
        }

        public virtual void Render(TextWriter writer)
        {
            RenderTagBegin(writer);
            RenderAttributes(writer);

            if (IsPairTag)
            {
                RenderOpenTagEnd(writer);
                RenderContent(writer);
                RenderChildren(writer);
                RenderCloseTag(writer);
            }
            else
            {
                writer.Write(" />");
            }
        }

        public string Type { get; }

        public List<IAttribute> Attributes { get; } = new List<IAttribute>();

        public List<IRenderable> Children { get; } = new List<IRenderable>();

        public string Content { get; set; }

        protected virtual void RenderTagBegin(TextWriter writer)
        {
            writer.Write("<");
            writer.Write(Type);
        }

        protected virtual void RenderAttributes(TextWriter writer)
        {
            foreach (var attribute in Attributes)
            {
                RenderAttribute(writer, attribute);
            }
        }

        protected virtual void RenderAttribute(TextWriter writer, IAttribute attribute)
        {
            writer.Write(" ");
            writer.Write(attribute.Name);

            if (!string.IsNullOrWhiteSpace(attribute.Value))
            {
                writer.Write("=\"");
                writer.Write(attribute.Value);
                writer.Write("\"");
            }
        }

        protected virtual void RenderOpenTagEnd(TextWriter writer)
        {
            writer.Write(">");
        }

        protected virtual void RenderChildren(TextWriter writer)
        {
            foreach (var child in Children)
            {
                child.Render(writer);
            }
        }

        protected virtual void RenderContent(TextWriter writer)
        {
            if (!string.IsNullOrWhiteSpace(Content))
            {
                writer.Write(HttpUtility.HtmlEncode(Content));
            }
        }

        protected virtual void RenderCloseTag(TextWriter writer)
        {
            writer.Write($"</{Type}>");
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            using (var writer = new StringWriter())
            {
                Render(writer);
            }

            return sb.ToString();
        }
    }
}
