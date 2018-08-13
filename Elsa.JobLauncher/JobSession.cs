using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common;
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

        public bool VerifyPassword(string hash, string password, bool isDefault)
        {
            if (isDefault)
            {
                throw new InvalidOperationException("Not accessible with default password");
            }

            return PasswordHashHelper.Verify(password, hash);
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
