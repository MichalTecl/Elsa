using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common;
using Elsa.Core.Entities.Commerce.Common.Security;

using Robowire.RoboApi;
using Robowire.RobOrm.Core;

namespace Elsa.Users
{
    [Controller("user")]
    public class UserController : ElsaControllerBase
    {
        private readonly IDatabase m_database;

        public UserController(ISession session, IDatabase database)
            : base(session)
        {
            m_database = database;
        }

        [AllowAnonymous]
        public ISession Login(string user, string password)
        {
            Session.Login(user, password);

            return Session;
        }

        [AllowAnonymous]
        public ISession GetCurrentSession()
        {
            return Session;
        }

        [AllowWithDefaultPassword]
        public ISession ChangePassword(string oldPassword, string newPassword)
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
                var user = m_database.SelectFrom<IUser>().Where(i => i.Id == Session.User.Id).Execute().FirstOrDefault();
                if (user == null)
                {
                    return null;
                }

                if (!Session.VerifyPassword(user.PasswordHash, oldPassword, user.UsesDefaultPassword))
                {
                    throw new InvalidOperationException("Staré heslo není platné");
                }
                
                user.PasswordHash = PasswordHashHelper.Hash(newPassword);
                user.UsesDefaultPassword = false;

                m_database.Save(user);

                Session.Logout();
                Session.Login(user.EMail, newPassword);

                Session.Logout();

                tran.Commit();
            }

            return Session;
        }

        [AllowWithDefaultPassword]
        public void Logout()
        {
            Session.Logout();
        }


    }
}
