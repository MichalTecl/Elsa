using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Smtp.Core;
using Robowire.RobOrm.Core;

namespace Elsa.Users.Components
{
    public class UserManagementFacade : IUserManagementFacade
    {
        private const string c_ranPassChars = "23456789abcdefghklmnpqrstuvwxyzABCDEFGHKLMNPRSTUVWXYZ!?";
        private readonly Random m_random = new Random();

        private readonly ISession m_session;
        private readonly IUserRepository m_userRepository;
        private readonly IMailSender m_mailSender;
        private readonly IDatabase m_database;
        private readonly ILog m_log;
        private readonly ICache m_cache;

        public UserManagementFacade(ISession session, IUserRepository userRepository, IMailSender mailSender, IDatabase database, ILog log, ICache cache)
        {
            m_session = session;
            m_userRepository = userRepository;
            m_mailSender = mailSender;
            m_database = database;
            m_log = log;
            m_cache = cache;
        }

        public void InviteUser(string email)
        {
            email = (email ?? string.Empty).Trim();

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                if (addr.Address != email)
                {
                    throw new InvalidOperationException($"Email address {email} converted to {addr.Address}");
                }
            }
            catch (Exception ex)
            {
                m_log.Error($"InviteUser failed email={email}", ex);
                throw new InvalidOperationException("Neplatná e-mailová adresa");
            }
            
            using (var tx = m_database.OpenTransaction())
            {
                var thePass = GeneratePass(6);
                m_userRepository.CreateUserAccount(email, thePass);

                m_mailSender.Send(email, "Pozvánka do systému ELSA", $"Uživatel {m_session.User.EMail} Vás pozval do systému ELSA. \r\n Vaše dočasné heslo je: {thePass}\r\n Přihlaste se na {m_session.Project.HomeUrl}\r\nPozor! Vaše dočasné heslo je třeba po přihlášení změnit (kliknutím na link '{email}' v pravém horním rohu), do té doby nebudete moci Elsu používat.");

                tx.Commit();
            }
        }

        public void ResetPassword(int userId)
        {
            var newPass = GeneratePass(6);

            m_userRepository.UpdateUser(userId, user =>
            {
                user.UsesDefaultPassword = true;
                user.PasswordHash = newPass;

                m_mailSender.Send(user.EMail, "Reset hesla do systému ELSA",
                    $"Vaše dočasné heslo je: {newPass}\r\nPozor! Dočasné heslo je třeba po přihlášení změnit (kliknutím na link '{user.EMail}' v pravém horním rohu), do té doby nebudete moci Elsu používat.");
            });
        }

        public void SetAccountLocked(int userId, bool locked)
        {
            m_userRepository.UpdateUser(userId, user =>
            {
                if ((user.LockDt != null) == locked)
                {
                    return;
                }

                if (locked)
                {
                    user.LockDt = DateTime.Now;
                    user.LockUserId = m_session.User.Id;
                }
                else
                {
                    user.LockDt = null;
                    user.LockUserId = null;
                }
            });
        }

        private string GeneratePass(int length)
        {
            var sb = new StringBuilder(length);

            for (var i = 0; i < length; i++)
            {
                sb.Append(c_ranPassChars[m_random.Next(c_ranPassChars.Length)]);
            }

            return sb.ToString();
        }

        
    }
}
