namespace Elsa.Common.Interfaces
{
    public sealed class UserRight
    {
        public UserRight(string symbol, string description, UserRight extends = null)
        {
            Description = description;
            Symbol = symbol;
            Extends = extends;
        }
        
        public string Symbol { get; }

        public string Description { get; }

        public UserRight Extends { get; }

        public override bool Equals(object obj)
        {
            return (obj as UserRight)?.Symbol.Equals(Symbol) == true;
        }

        public override int GetHashCode()
        {
            return Symbol.GetHashCode();
        }
    }
}
