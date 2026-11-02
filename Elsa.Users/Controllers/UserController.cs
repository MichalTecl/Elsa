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
using RobOrm.Core;

namespace Elsa.Users.Controllers
{
    [Controller("user")]
    public class UserController : ElsaControllerBase
    {
        private readonly IDatabase _database;
        private readonly ILog _log;
        private readonly IUserRepository _repository;
        private readonly ISession _session;
        private readonly IUserManagementFacade _managementFacade;
        private readonly ICache _cache;

        public UserController(IWebSession webSession, ILog log, IDatabase database, IUserRepository repository, IUserManagementFacade managementFacade, ICache cache)
            : base(webSession, log)
        {
            _session = webSession;
            _log = log;
            _database = database;
            _repository = repository;
            _managementFacade = managementFacade;
            _cache = cache;
        }

        [DoNotLogParams]
        [AllowAnonymous]
        public IWebSession Login(string user, string password)
        {
            try
            {
                _log.Info($"Login requested for user {user}");

                WebSession.Login(user, password);

                if (WebSession.User == null)
                {
                    _log.Error($"Login failed for user {user}");
                    return WebSession;
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Login failed for user {user}", ex);
                throw;
            }

            _log.Info($"{user} successfully logged in");

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
            var userId = _session.User.Id;

            oldPassword = oldPassword.Trim();
            newPassword = newPassword.Trim();

            if (oldPassword == newPassword)
            {
                throw new InvalidOperationException("Nové heslo nesmí být stejné jako staré");
            }

            if (newPassword.Length < 6)
            {
                throw new InvalidOperationException("Heslo musí mít alespoň 6 znaků");
            }
            
            using (var tran = _database.OpenTransaction())
            {
                var user = _database.SelectFrom<IUser>().Where(i => i.Id == WebSession.User.Id).Execute().FirstOrDefault();
                if (user == null)
                {
                    return null;
                }

                if (!WebSession.VerifyPassword(user.PasswordHash, oldPassword, user.UsesDefaultPassword))
                {
                    throw new InvalidOperationException("Staré heslo není platné");
                }

                var history = _repository.GetPasswordHistory(user.Id);
                if (history.Any(h => PasswordHashHelper.Verify(newPassword, h.PasswordHash)))
                    throw new InvalidOperationException("Toto heslo již bylo používáno, zvolte prosím jiné");

                _repository.UpdateUser(user.Id, u => {
                    u.PasswordHash = PasswordHashHelper.Hash(newPassword);
                    u.UsesDefaultPassword = false;
                });
                                                                
                WebSession.Logout();
                WebSession.Login(user.EMail, newPassword);

                WebSession.Logout();

                tran.Commit();
            }

            _repository.InvalidateUserCache(userId);

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
            var users = _repository.GetAllUsers().Where(u => u.Id != _session.User.Id)
                .Where(u => _repository.GetCanManage(_session.User.Id, u.Id)).OrderBy(u => u.LockDt == null ? 0 : 1).ThenBy(u => u.EMail).Select(u =>
                    new UserViewModel()
                    {
                        Id = u.Id,
                        Name = u.EMail,
                        IsActive = !u.UsesDefaultPassword,
                        IsLocked = u.LockDt != null,
                        IsOnline = GetIsUserOnline(u.Id)
                    });

            return users;
        }

        private bool GetIsUserOnline(int id)
        {
            var kkey = $"userOnline_{id}";
            if (!_cache.KeyExists(kkey))
                return false;

            if (_cache.ReadThrough<DateTime?>(kkey, TimeSpan.FromMinutes(10), () => null) == null) 
            {
                _cache.Remove(kkey);
                return false;
            }

            return true;
        }

        public IEnumerable<UserViewModel> InviteUser(string email)
        {
            _managementFacade.InviteUser(email);

            return GetAllUsersExceptMe();
        }

        public IEnumerable<UserViewModel> ResetPassword(int userId)
        {
            _managementFacade.ResetPassword(userId);

            return GetAllUsersExceptMe();
        }

        public IEnumerable<UserViewModel> ToggleAccountLock(int userId, bool locked)
        {
            _managementFacade.SetAccountLocked(userId, locked);

            return GetAllUsersExceptMe();
        }
    }
}
