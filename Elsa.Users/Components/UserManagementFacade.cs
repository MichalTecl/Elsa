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
        private const string RANDOM_PASSWORD_CHARACTERS = "23456789abcdefghklmnpqrstuvwxyzABCDEFGHKLMNPRSTUVWXYZ!?";
        private readonly Random _random = new Random();

        private readonly ISession _session;
        private readonly IUserRepository _userRepository;
        private readonly IMailSender _mailSender;
        private readonly IDatabase _database;
        private readonly ILog _log;
        private readonly ICache _cache;

        public UserManagementFacade(ISession session, IUserRepository userRepository, IMailSender mailSender, IDatabase database, ILog log, ICache cache)
        {
            _session = session;
            _userRepository = userRepository;
            _mailSender = mailSender;
            _database = database;
            _log = log;
            _cache = cache;
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
                _log.Error($"InviteUser failed email={email}", ex);
                throw new InvalidOperationException("Neplatná e-mailová adresa");
            }
            
            using (var tx = _database.OpenTransaction())
            {
                var thePass = GeneratePass(6);
                _userRepository.CreateUserAccount(email, thePass);

                _mailSender.Send(SenderMailboxType.SystemRobot, email, "Pozvánka do systému ELSA", $"Uživatel {_session.User.EMail} Vás pozval do systému ELSA. \r\n Vaše dočasné heslo je: {thePass}\r\n Přihlaste se na {_session.Project.HomeUrl}\r\nPozor! Vaše dočasné heslo je třeba po přihlášení změnit (kliknutím na link '{email}' v pravém horním rohu), do té doby nebudete moci Elsu používat.");

                tx.Commit();
            }
        }

        public void ResetPassword(int userId)
        {
            var newPass = GeneratePass(6);

            _userRepository.UpdateUser(userId, user =>
            {
                user.UsesDefaultPassword = true;
                user.PasswordHash = newPass;
                                
                _mailSender.Send(SenderMailboxType.SystemRobot, user.EMail, "Reset hesla do systému ELSA",
                    $"Vaše dočasné heslo je: {newPass}\r\nPozor! Dočasné heslo je třeba po přihlášení změnit (kliknutím na link '{user.EMail}' v pravém horním rohu), do té doby nebudete moci Elsu používat.");
            });
        }

        public void SetAccountLocked(int userId, bool locked)
        {
            _userRepository.UpdateUser(userId, user =>
            {
                if ((user.LockDt != null) == locked)
                {
                    return;
                }

                if (locked)
                {
                    user.LockDt = DateTime.Now;
                    user.LockUserId = _session.User.Id;
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
                sb.Append(RANDOM_PASSWORD_CHARACTERS[_random.Next(RANDOM_PASSWORD_CHARACTERS.Length)]);
            }

            return sb.ToString();
        }
    }
}
