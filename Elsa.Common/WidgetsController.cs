using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Core.Entities.Commerce.Common.Widgets;

using Robowire.RoboApi;
using Robowire.RobOrm.Core;

namespace Elsa.Common
{
    [Controller("widgets")]
    public class WidgetsController : ElsaControllerBase
    {
        private readonly IDatabase m_database;

        public WidgetsController(ISession session, IDatabase database)
            : base(session)
        {
            m_database = database;
        }

        [AllowAnonymous]
        public IEnumerable<IAppWidget> GetWidgets()
        {
            var widgetsQuery = m_database.SelectFrom<IAppWidget>().OrderBy(w => w.ViewOrder);

            if (Session?.User == null || Session.User.UsesDefaultPassword)
            {
                widgetsQuery.Where(w => w.IsAnonymous);
            }
            
            return widgetsQuery.Execute();
        }
    }
}
