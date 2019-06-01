namespace Elsa.Common.Noml.Core
{
    public sealed class Attribute : IAttribute
    {
        public Attribute(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public string Value { get; set; }
    }
}
