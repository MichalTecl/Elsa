using System;

namespace Elsa.Smtp.Core
{
    public class SenderMailboxType
    {
        public static readonly SenderMailboxType SystemRobot = new SenderMailboxType(nameof(SystemRobot), s => new SmtpSettingsHolder 
        {
            SmtpHost = s.SmtpHost,
            SmtpPort = s.SmtpPort,
            SenderAddress = s.SenderAddress,
            SenderName = s.SenderName,
            SenderPassword = s.SenderPassword,
        });

        public static readonly SenderMailboxType CustomerFacingSender = new SenderMailboxType(nameof(CustomerFacingSender), s => new SmtpSettingsHolder
        {
            SmtpHost = s.CustomerFacingSmtpHost,
            SmtpPort = s.CustomerFacingSmtpPort,
            SenderAddress = s.CustomerFacingSenderAddress,
            SenderName = s.CustomerFacingSenderName,
            SenderPassword = s.CustomerFacingSenderPassword,
        });

        private Func<SmtpSettings, ISmtpSettings> _mapper;

        private SenderMailboxType(string typeName, Func<SmtpSettings, ISmtpSettings> mapper)
        {
            TypeName = typeName;
            _mapper = mapper;
        }

        public string TypeName { get; }

        internal ISmtpSettings MapSettings(SmtpSettings allSettings)
        {
            return _mapper(allSettings);
        }

        private sealed class SmtpSettingsHolder : ISmtpSettings
        {
            public string SmtpHost { get; set; }

            public int SmtpPort { get; set; }

            public string SenderAddress { get; set; }

            public string SenderName { get; set; }

            public string SenderPassword { get; set; }
        }
    }
}
