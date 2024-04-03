namespace Robowire.RobOrm.Core
{
    public interface IDbTypeAttribute
    {
        string ColumnDeclarationTypeText { get; }

        bool IsNullable { get; }
    }
}
