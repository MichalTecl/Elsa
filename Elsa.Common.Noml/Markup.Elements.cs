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
        public static IElement B(params object[] nodes)
        {
            return new Element("B", nodes);
        }

        public static IElement Body(params object[] nodes)
        {
            return new Element("Body", nodes);
        }

        public static IElement Head(params object[] nodes)
        {
            return new Element("Head", nodes);
        }

        public static IElement H1(params object[] nodes)
        {
            return new Element("H1", nodes);
        }

        public static IElement H2(params object[] nodes)
        {
            return new Element("H2", nodes);
        }

        public static IElement H3(params object[] nodes)
        {
            return new Element("H3", nodes);
        }

        public static IElement H4(params object[] nodes)
        {
            return new Element("H4", nodes);
        }

        public static IElement H5(params object[] nodes)
        {
            return new Element("H5", nodes);
        }

        public static IElement H6(params object[] nodes)
        {
            return new Element("H6", nodes);
        }

        public static IElement I(params object[] nodes)
        {
            return new Element("I", nodes);
        }

        public static IElement Label(params object[] nodes)
        {
            return new Element("Label", nodes);
        }

        public static IElement P(params object[] nodes)
        {
            return new Element("P", nodes);
        }

        public static IElement Script(params object[] nodes)
        {
            return new Element("Script", nodes);
        }

        public static IElement Span(params object[] nodes)
        {
            return new Element("Span", nodes);
        }

        public static IElement Table(params object[] nodes)
        {
            return new Element("Table", nodes);
        }

        public static IElement Td(params object[] nodes)
        {
            return new Element("Td", nodes);
        }

        public static IElement Th(params object[] nodes)
        {
            return new Element("Th", nodes);
        }

        public static IElement Thead(params object[] nodes)
        {
            return new Element("Thead", nodes);
        }

        public static IElement Tbody(params object[] nodes)
        {
            return new Element("Tbody", nodes);
        }

        public static IElement Tr(params object[] nodes)
        {
            return new Element("Tr", nodes);
        }

        public static IElement Div(params object[] nodes)
        {
            return new Element("Div", nodes);
        }
    }
}
