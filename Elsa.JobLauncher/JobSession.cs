using System;
using System.Linq;

using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;

using Robowire.RobOrm.Core;

namespace Elsa.JobLauncher
{
    public class JobSession : ISession
    {
        private IDatabase m_database;

        public JobSession(IDatabase database)
        {
            m_database = database;
        }

        public IUser User { get; private set; }

        public IProject Project { get; private set; }

        public long? SessionId => null; //TODO

        public bool VerifyPassword(string hash, string password, bool isDefault)
        {
            if (isDefault)
            {
                throw new InvalidOperationException("Not accessible with default password");
            }

            return PasswordHashHelper.Verify(password, hash);
        }

        public string Release => DateTime.Now.ToString();
        public string Culture => "cs-cz";
        public bool HasUserRight(UserRight right)
        {
            return true;
        }

        public bool HasUserRight(string symbol)
        {
            return true;
        }

        public void Login(string user, string password)
        {
            var userRecord = m_database.SelectFrom<IUser>().Where(i => i.EMail == user).Join(u => u.Project).Execute().FirstOrDefault();

            if (userRecord == null || !VerifyPassword(userRecord.PasswordHash, password, userRecord.UsesDefaultPassword))
            {
                throw new InvalidOperationException("Invalid login");
            }

            User = userRecord;
            Project = userRecord.Project;

        }
    }
}
