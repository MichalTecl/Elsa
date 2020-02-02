namespace Elsa.Common.Interfaces
{
    public sealed class UserRight
    {
        public UserRight(string symbol, string description, params UserRight[] extends)
        {
            Description = description;
            Symbol = symbol;
            Extends = extends;
        }
        
        public string Symbol { get; }

        public string Description { get; }

        public UserRight[] Extends { get; }
    }
}
