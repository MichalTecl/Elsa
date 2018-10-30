using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Common.Security;

using Robowire.RobOrm.Core;

namespace Elsa.Common.UserRightsInfrastructure
{
    public class SyncUserRightsJob : IStartupJob
    {
        private readonly IDatabase m_database;

        public SyncUserRightsJob(IDatabase database)
        {
            m_database = database;
        }

        public bool IsExceptionFatal { get; } = true;

        public void Execute()
        {
            var dbRights = m_database.SelectFrom<IUserRight>().Execute().ToList();

            var codeRights = UserRightDefinitionCollector.GetAllUserRights();

            foreach (var codeRight in codeRights)
            {
                var dbRight =
                    dbRights.FirstOrDefault(
                        r => r.Symbol.Equals(codeRight.Name, StringComparison.InvariantCultureIgnoreCase));

                
                if (dbRight == null)
                {
                    dbRight = m_database.New<IUserRight>();
                    dbRight.Symbol = codeRight.Name;
                }
                else if (dbRight.FullPath == codeRight.FullName && dbRight.Description == codeRight.Description)
                {
                    continue;
                }

                dbRight.Description = codeRight.Description;
                dbRight.FullPath = codeRight.FullName;
                
                m_database.Save(dbRight);
            }

        }
    }
}
