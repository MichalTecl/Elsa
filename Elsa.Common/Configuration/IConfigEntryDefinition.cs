namespace Elsa.Common.Configuration
{
    public interface IConfigEntryDefinition
    {
        string Key { get; }

        string DefaultValueJson { get; }

        ConfigEntryScope[] Scope { get; }
    }
}
