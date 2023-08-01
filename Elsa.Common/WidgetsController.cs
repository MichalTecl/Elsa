using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Common.Widgets;

using Robowire.RoboApi;
using Robowire.RobOrm.Core;

namespace Elsa.Common
{
    [Controller("widgets")]
    public class WidgetsController : ElsaControllerBase
    {
        private readonly IDatabase m_database;
        private readonly ICache m_cache;

        public WidgetsController(IWebSession webSession, IDatabase database, ILog log, ICache cache)
            : base(webSession, log)
        {
            m_database = database;
            m_cache = cache;
        }

        [AllowAnonymous]
        public IEnumerable<IAppWidget> GetWidgets()
        {
            var widgets = GetAllWidgets().OrderBy(w => w.ViewOrder);

            foreach(var widget in widgets) 
            {
                if (WebSession?.User == null || WebSession.User.UsesDefaultPassword) 
                {
                    if (widget.IsAnonymous)
                    {
                        yield return widget;
                        continue;
                    }    
                }

                if(!string.IsNullOrWhiteSpace(widget.UserRightSymbol))
                {
                    if (!WebSession.HasUserRight(widget.UserRightSymbol))
                        continue;
                }

                yield return widget;
            }
        }
                
        private List<IAppWidget> GetAllWidgets()
        {
            return m_cache.ReadThrough("all_widgets",
                TimeSpan.FromHours(1),
                () => m_database.SelectFrom<IAppWidget>().Execute().ToList()
            );
        }
    }
}
