
namespace Elsa.Smtp.Core
{
    public interface ISmtpSettings
    {
        string SmtpHost { get; }
        int SmtpPort { get; }
        string SenderAddress { get; }
        string SenderName { get; }
        string SenderPassword { get; }
    }
}
