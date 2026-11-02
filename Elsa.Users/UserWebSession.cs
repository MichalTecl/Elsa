using System;
using System.Linq;
using System.Web;
using System.Web.Routing;

using Elsa.Common;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;

using Robowire.RoboApi;
using RobOrm.Core;

namespace Elsa.Users
{
    public class UserWebSession : IWebSession
    {
        private const string c_sessionCookieIdentifier = "elsa_sid";
        private bool _initialized;
        private Guid? _sessionPublicId;
        private readonly ICache _cache;
        private readonly Lazy<IUserRepository> _userRepository;

        private readonly IDatabase _database;

        private IUserSession _session;

        public UserWebSession(IDatabase database, ICache cache, Lazy<IUserRepository> userRepository)
        {
            _database = database;
            _cache = cache;
            _userRepository = userRepository;
        }

        public IUser User
        {
            get
            {
                EnsureInitialized();
                _cache.ReadThrough<DateTime?>($"userOnline_{_session?.UserId}", TimeSpan.FromMinutes(10), () => DateTime.Now);

                return _session?.User;
            }
        }

        public IProject Project
        {
            get
            {
                EnsureInitialized();
                return _session?.Project;
            }
        }

        public long? SessionId => _session?.Id;

        public void Login(string user, string password)
        {
            EnsureInitialized();

            user = user?.Trim();
            password = password?.Trim();

            if (_session?.User != null)
            {
                throw new InvalidOperationException("User already loged in");
            }

            var userRecord = _database.SelectFrom<IUser>().Where(u => u.EMail == user).Execute().FirstOrDefault();
            if (userRecord == null)
            {
                return;
            }

            if (userRecord.LockDt != null)
            {
                throw new InvalidOperationException("Uživatelský účet byl zablokován");
            }

            if (!VerifyPassword(userRecord.PasswordHash, password, userRecord.UsesDefaultPassword))
            {
                return;
            }

            _session = _database.New<IUserSession>();
            _session.PublicId = _sessionPublicId ?? Guid.Empty;
            _session.UserId = userRecord.Id;
            _session.ProjectId = userRecord.ProjectId;
            _session.StartDt = DateTime.Now;
            _session.EndDt = null;
            
            _database.Save(_session);

            _session =
                _database.SelectFrom<IUserSession>().Where(s => s.Id == _session.Id).Join(s => s.User).Join(s => s.Project).Execute().FirstOrDefault();
        }

        public void Logout()
        {
            EnsureInitialized();

            if (User != null)
            {
                _cache.Remove($"userOnline_{_session?.UserId}");
                _cache.Remove(GetCacheKey(_session.PublicId));
                _session.EndDt = DateTime.Now;
                _database.Save(_session);
                _session = null;
            }
        }

        public bool VerifyPassword(string hash, string password, bool isDefault)
        {
            if (isDefault)
            {
                return hash == password;
            }

            return PasswordHashHelper.Verify(password, hash);
        }

        public string Release => ReleaseVersionInfo.Tag;
        public string Culture => "cs-cz";
        public bool HasUserRight(UserRight right)
        {
            return HasUserRight(right.Symbol);
        }

        public bool HasUserRight(string symbol)
        {
            if (User == null)
            {
                return false;
            }
            
            return _userRepository.Value.GetUserRights(User.Id).Contains(symbol);
        }

        public void EnsureUserRight(UserRight right)
        {
            if (!HasUserRight(right))
            {
                throw new InvalidOperationException($"Uživatel nemá oprávnění provést požadovanou akci ({right.Description})");
            }
        }

        public string[] UserRights
        {
            get
            {
                if (User == null)
                {
                    return new string[0];
                }

                return _userRepository.Value.GetUserRights(User.Id).ToArray();
            }
        }

        public void Initialize(HttpContext rq) 
        {
            Initialize(ck => rq.Request.Cookies[ck], c => rq.Response.Cookies.Add(c), ShouldSuppressCookieWrite(rq.Items));
        }

        public void Initialize(HttpContextBase rq) 
        {
            Initialize(ck => rq.Request.Cookies[ck], c => rq.Response.Cookies.Add(c), ShouldSuppressCookieWrite(rq.Items));
        }

        private void Initialize(Func<string, HttpCookie> getCookie, Action<HttpCookie> setCookie, bool suppressCookieWrite)
        {
            var sessionCookie = getCookie(c_sessionCookieIdentifier);
            if (sessionCookie != null)
            {
                Guid sid;
                if (Guid.TryParse(sessionCookie.Value, out sid))
                {
                    _session = _cache.ReadThrough(GetCacheKey(sid),
                        TimeSpan.FromMinutes(1),
                        () =>
                            _database.SelectFrom<IUserSession>()
                                .Join(s => s.User)
                                .Join(s => s.Project)
                                .Take(1)
                                .Where(s => (s.PublicId == sid) && (s.EndDt == null))
                                .Execute()
                                .FirstOrDefault());

                    if (_session == null)
                    {
                        sessionCookie = null;
                    }
                }
                else
                {
                    sessionCookie = null;
                }
            }

            if (sessionCookie == null)
            {
                _session = null;

                if (suppressCookieWrite)
                {
                    _initialized = true;
                    return;
                }

                _sessionPublicId = Guid.NewGuid();
                _cache.Remove(GetCacheKey(_sessionPublicId.Value));

                sessionCookie = new HttpCookie(c_sessionCookieIdentifier, _sessionPublicId.ToString())
                                    {
                                        Expires = DateTime.Now
                                    };
            }

            if (!suppressCookieWrite)
            {
                sessionCookie.Expires = DateTime.Now.AddDays(30);
                setCookie(sessionCookie);
            }
            
            _initialized = true;
        }

        private static bool ShouldSuppressCookieWrite(System.Collections.IDictionary items)
        {
            return RequestContextFlags.GetSuppressSessionCookieWrite(items);
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Session was not initialized");
            }
        }

        private string GetCacheKey(Guid sid)
        {
            return $"usix_{sid}";
        }
    }
}



