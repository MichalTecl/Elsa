using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common.Noml.Core;

namespace Elsa.Common.Noml
{
    public partial class Markup
    {
        public static IElement Html(Action<IElement> head, Action<IElement> body)
        {
            var html = new Element("HTML", Attr("xmlns", "http://www.w3.org/1999/xhtml"));

            head(html.NewNode(Element("head", NonPairTagElement("meta", Attr("charset", "utf-8")))));
            body(html.NewNode(Element("body")));

            return html;
        }

        public static IElement Element(string name, params object[] content)
        {
            return new Element(name, content);
        }

        public static IElement NonPairTagElement(string name, params IAttribute[] attributes)
        {
            var element = new Element(name, attributes)
            {
                IsPairTag = false
            };

            return element;
        }
        
        public static IAttribute Attr(string name, string value)
        {
            return new Core.Attribute(name, value);
        }

        public static IElement CssLink(string url)
        {
            return NonPairTagElement("link", Attr("href", url), Attr("rel", "stylesheet"));
        }
    }
}
