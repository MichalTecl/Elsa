namespace Elsa.Common.UserRightsInfrastructure
{
    public sealed class UserRight
    {
        public UserRight(string description)
        {
            Description = description;
        }
        
        public string Description { get; }
    }
}
