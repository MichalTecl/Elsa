using System.IO;

using Elsa.Common.Noml.Core;

namespace Elsa.Common.Noml.Forms
{
    public class Paper : Markup, IRenderable
    {
        private readonly string m_title;

        private readonly IRenderable[] m_forms;

        public Paper(string title, params IRenderable[] forms)
        {
            m_title = title;
            m_forms = forms;
        }

        public void Render(TextWriter writer)
        {
            writer.Write("<!DOCTYPE html>\n");
            Html(head =>
                {
                    head.NewNode(Element("title", m_title));
                    head.NewNode(CssLink("/Style/Paperforms.css"));
                }
                , body => body.NewNode(
                    Div(Class("paperFrame"),
                        Div(Class("paper"), m_forms)))).Render(writer);
        }
    }
}
