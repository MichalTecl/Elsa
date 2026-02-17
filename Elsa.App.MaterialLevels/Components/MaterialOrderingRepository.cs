using Elsa.App.MaterialLevels.Entities;
using Elsa.Common.Interfaces;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.MaterialLevels.Components
{
    public class MaterialOrderingRepository
    {
        private readonly IDatabase _database;
        private readonly ISession _session;

        public MaterialOrderingRepository(IDatabase database, ISession session)
        {
            _database = database;
            _session = session;
        }

        internal void SetOrderDeliveryDeadline(int materialId, DateTime? date)
        {
            var evt = _database.SelectFrom<IMaterialOrderEvent>()
                .Where(e => e.MaterialId == materialId)
                .OrderByDesc(e => e.OrderDt)
                .Take(1)                
                .Execute()
                .FirstOrDefault() ?? throw new ArgumentException("Order event not found");

            if (date == null)
            {
                _database.Delete(evt);
                return;
            }

            evt.DeliveryDeadline = date;
            evt.DeadlineSetDt = DateTime.Now;
            evt.DeadlineSetUserId = _session.User.Id;

            _database.Save(evt);
        }
    }
}
