using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Apps.Common;
using Elsa.Apps.CommonData.Model;
using Elsa.Commerce.Core.Repositories;
using Elsa.Common;
using Elsa.Common.Logging;
using Elsa.EditorBuilder;
using Elsa.EditorBuilder.Internal;

using Robowire.RoboApi;

namespace Elsa.Apps.CommonData
{
    [Controller("fixedCostTypes")]
    public class FixedCostTypeController : AutoControllerBase<FixedCostTypeViewModel>
    {
        private readonly IFixedCostRepository m_repository;

        public FixedCostTypeController(IWebSession webSession, ILog log, IFixedCostRepository repository)
            : base(webSession, log)
        {
            m_repository = repository;
        }

        public override EntityListingPage<FixedCostTypeViewModel> List(string pageKey)
        {
            return new EntityListingPage<FixedCostTypeViewModel>(m_repository.GetFixedCostTypes().Select(c => new FixedCostTypeViewModel(c)));
        }

        public override FixedCostTypeViewModel Save(FixedCostTypeViewModel entity)
        {
            var saved = m_repository.SetFixedCostType(entity.Id, entity.Name, entity.Percent);

            return new FixedCostTypeViewModel(saved);
        }

        public override FixedCostTypeViewModel Get(FixedCostTypeViewModel uidHolder)
        {
            if (uidHolder.Id == null)
            {
                throw new ArgumentException("id == null");
            }

            var entity = m_repository.GetFixedCostTypes().FirstOrDefault(c => c.Id == uidHolder.Id.Value);
            if (entity == null)
            {
                return null;
            }

            return new FixedCostTypeViewModel(entity);
        }

        public override FixedCostTypeViewModel New()
        {
            return new FixedCostTypeViewModel();
        }

        protected override IDefineGrid<FixedCostTypeViewModel> SetUidProperty(ISetIdProperty<FixedCostTypeViewModel> setter)
        {
            return setter.WithIdProperty(ct => ct.Id);
        }

        protected override void SetupGrid(GridBuilder<FixedCostTypeViewModel> gridBuilder)
        {
            gridBuilder
                .Column(CellClass.Cell10, s => s.Name)
                .Column(CellClass.Cell10, s => s.Percent);
        }

        protected override void SetupForm(IFormBuilder<FixedCostTypeViewModel> formBuilder)
        {
            formBuilder.Div("fctForm", d => d.Field(s => s.Name).Field(s => s.Percent));
        }
    }
}
