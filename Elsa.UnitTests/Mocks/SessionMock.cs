using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;

namespace Elsa.UnitTests.Mocks
{
    internal class SessionMock : ISession
    {
        public SessionMock()
        {
            Project = new ProjectMock();
        }

        public IUser User { get; }

        public IProject Project { get; }

        public long? SessionId { get; }

        public bool VerifyPassword(string hash, string password, bool isDefault)
        {
            throw new NotImplementedException();
        }
    }

    internal class ProjectMock : IProject
    {
        public int Id => 0;

        public string Name { get; set; }

        public IEnumerable<IUser> Users { get; }
    }
}
