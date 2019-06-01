using Elsa.Common.Noml.Core;

namespace Elsa.Common.Noml
{
    public partial class Markup
    {
        public static IAttribute Class(string value)
        {
            return new Attribute("class", value);
        }

        public static IAttribute Id(string value)
        {
            return new Attribute("id", value);
        }

        public static IAttribute Style(string value)
        {
            return new Attribute("style", value);
        }
    }
}
