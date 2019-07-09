using System.IO;


namespace Elsa.Common.Noml.Core
{
    internal class Literal : IRenderable
    {
        private readonly string m_html;

        public Literal(string html)
        {
            m_html = html;
        }

        public void Render(TextWriter writer)
        {
            writer.Write(m_html);
        }
    }
}
