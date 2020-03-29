using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Common.Security;
using Elsa.Users.ViewModel;
using Robowire.RoboApi;
using Robowire.RobOrm.Core;

namespace Elsa.Users.Controllers
{
    [Controller("user")]
    public class UserController : ElsaControllerBase
    {
        private readonly IDatabase m_database;
        private readonly ILog m_log;
        private readonly IUserRepository m_repository;
        private readonly ISession m_session;
        private readonly IUserManagementFacade m_managementFacade;
        private readonly ICache m_cache;

        public UserController(IWebSession webSession, ILog log, IDatabase database, IUserRepository repository, IUserManagementFacade managementFacade, ICache cache)
            : base(webSession, log)
        {
            m_session = webSession;
            m_log = log;
            m_database = database;
            m_repository = repository;
            m_managementFacade = managementFacade;
            m_cache = cache;
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


        public IEnumerable<string> GetAllUserNamesExceptMe()
        {
            return GetAllUsersExceptMe().Select(u => u.Name);
        }

        public IEnumerable<UserViewModel> GetAllUsersExceptMe()
        {
            var users = m_repository.GetAllUsers().Where(u => u.Id != m_session.User.Id)
                .Where(u => m_repository.GetCanManage(m_session.User.Id, u.Id)).OrderBy(u => u.LockDt == null ? 0 : 1).ThenBy(u => u.EMail).Select(u =>
                    new UserViewModel()
                    {
                        Id = u.Id,
                        Name = u.EMail,
                        IsActive = !u.UsesDefaultPassword,
                        IsLocked = u.LockDt != null
                    });

            return users;
        }
        
        public IEnumerable<UserViewModel> InviteUser(string email)
        {
            m_managementFacade.InviteUser(email);

            return GetAllUsersExceptMe();
        }

        public IEnumerable<UserViewModel> ResetPassword(int userId)
        {
            m_managementFacade.ResetPassword(userId);

            return GetAllUsersExceptMe();
        }

        public IEnumerable<UserViewModel> ToggleAccountLock(int userId, bool locked)
        {
            m_managementFacade.SetAccountLocked(userId, locked);

            return GetAllUsersExceptMe();
        }
    }
}
