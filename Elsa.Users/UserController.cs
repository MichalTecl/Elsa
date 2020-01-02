using System;
using System.Linq;

using Elsa.Common;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Common.Security;

using Robowire.RoboApi;
using Robowire.RobOrm.Core;

namespace Elsa.Users
{
    [Controller("user")]
    public class UserController : ElsaControllerBase
    {
        private readonly IDatabase m_database;
        private readonly ILog m_log;

        public UserController(IWebSession webSession, ILog log, IDatabase database)
            : base(webSession, log)
        {
            m_log = log;
            m_database = database;
        }

        [DoNotLogParams]
        [AllowAnonymous]
        public IWebSession Login(string user, string password)
        {
            try
            {
                m_log.Info($"Login requested for user {user}");

                WebSession.Login(user, password);

                if (WebSession.User == null)
                {
                    m_log.Error($"Login failed for user {user}");
                    return WebSession;
                }
            }
            catch (Exception ex)
            {
                m_log.Error($"Login failed for user {user}", ex);
                throw;
            }

            m_log.Info($"{user} successfully logged in");

            return WebSession;
        }

        [AllowAnonymous]
        public IWebSession GetCurrentSession()
        {
            return WebSession;
        }

        [DoNotLogParams]
        [AllowWithDefaultPassword]
        public IWebSession ChangePassword(string oldPassword, string newPassword)
        {
            oldPassword = oldPassword.Trim();
            newPassword = newPassword.Trim();

            if (oldPassword == newPassword)
            {
                return null;
            }

            if (newPassword.Length < 6)
            {
                throw new InvalidOperationException("Heslo musí mít alespoň 6 znaků");
            }
            
            using (var tran = m_database.OpenTransaction())
            {
                var user = m_database.SelectFrom<IUser>().Where(i => i.Id == WebSession.User.Id).Execute().FirstOrDefault();
                if (user == null)
                {
                    return null;
                }

                if (!WebSession.VerifyPassword(user.PasswordHash, oldPassword, user.UsesDefaultPassword))
                {
                    throw new InvalidOperationException("Staré heslo není platné");
                }
                
                user.PasswordHash = PasswordHashHelper.Hash(newPassword);
                user.UsesDefaultPassword = false;

                m_database.Save(user);

                WebSession.Logout();
                WebSession.Login(user.EMail, newPassword);

                WebSession.Logout();

                tran.Commit();
            }

            return WebSession;
        }

        [AllowWithDefaultPassword]
        public void Logout()
        {
            WebSession.Logout();
        }


    }
}
