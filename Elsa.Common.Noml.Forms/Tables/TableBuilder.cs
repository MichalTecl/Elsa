using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common.Noml.Core;

namespace Elsa.Common.Noml.Forms.Tables
{
    public class TableBuilder : IRenderable, ITableBuilder, IRowsBuilder
    {
        private readonly IElement m_table;
        private readonly IElement m_tbody;
        private IElement m_thead;
        
        public TableBuilder(params string[] classes)
        {
            m_table = Markup.Table(Markup.Class(string.Join(" ", classes)));
            m_tbody = new Element("TBODY");
            m_table.Children.Add(m_tbody);
        }
        
        public void Render(TextWriter writer)
        {
            m_table.Render(writer);
        }

        public IRowsBuilder Rows<T>(IEnumerable<T> collection, Func<T, object[]> generator)
        {
            foreach (var r in collection)
            {
                Row(generator(r));
            }

            return this;
        }

        public IRowsBuilder Row(params object[] values)
        {
            var tr = Markup.Tr();
            m_tbody.Children.Add(tr);

            foreach (var value in values)
            {
                tr.Children.Add(Markup.Td(value));
            }

            return this;
        }

        public IRowsBuilder Head(params string[] names)
        {
            m_thead = m_thead ?? m_table.NewNode(Markup.Element("THEAD"));

            var tr = Markup.Tr();
            m_thead.Children.Add(tr);

            foreach (var name in names)
            {
                tr.Children.Add(Markup.Th(name));
            }

            return this;
        }

        public string Type => m_table.Type;

        public List<IAttribute> Attributes => m_table.Attributes;

        public List<IRenderable> Children => m_table.Children;

        public string Content
        {
            get { return m_table.Content; }
            set { m_table.Content = value; }
        }
    }
}
