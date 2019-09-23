using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Commerce.Core.SaleEvents;
using Elsa.Common.Data;
using Elsa.Core.Entities.Commerce.Commerce.SaleEvents;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Common.Security;
using Robowire;

namespace Elsa.Commerce.Core.Adapters
{
    internal class SaleEventAdapter : AdapterBase<ISaleEvent>, ISaleEvent
    {
        public SaleEventAdapter(IServiceLocator serviceLocator, ISaleEvent adaptee) : base(serviceLocator, adaptee)
        {
        }

        public int Id => Adaptee.Id;

        public int ProjectId
        {
            get => Adaptee.ProjectId;
            set => Adaptee.ProjectId = value;
        }

        public IProject Project { get; }

        public string Name
        {
            get => Adaptee.Name;
            set => Adaptee.Name = value;
        }

        public DateTime EventDt
        {
            get => Adaptee.EventDt;
            set => Adaptee.EventDt = value;
        }

        public int UserId
        {
            get => Adaptee.UserId;
            set => Adaptee.UserId = value;
        }

        public IUser User => Get<IUserRepository, IUser>("User", r => r.GetUser(UserId));

        public IEnumerable<ISaleEventAllocation> Allocations =>
            Get<ISaleEventRepository, IEnumerable<ISaleEventAllocation>>("Allocations",
                r => r.GetAllocationsByEventId(Id));
    }
}
