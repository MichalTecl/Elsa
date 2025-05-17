using Elsa.App.Crm.Model;
using Elsa.App.Crm.Repositories.DynamicColumns.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Crm.Repositories.DynamicColumns
{
    public class TagsColumn : SimpleDynamicColumnBase
    {
        private readonly CustomerTagRepository _customerTagRepository;

        public TagsColumn(CustomerTagRepository customerTagRepository)
        {
            _customerTagRepository = customerTagRepository;
        }

        public override int DisplayOrder => 10;
        public override string Id => "Tags";

        public override string Title => "Štítky";

        public override string BoundProperty => "tags";

        public override string CellClass => "cell20";

        public override bool CanSort => false;

        public override void Populate(List<DistributorGridRowModel> rows)
        {
            var assignments = _customerTagRepository.GetAssignments(rows.Select(x => x.Id));

            foreach (var row in rows)
            {
                row.TagAssignments.AddRange(assignments.Where(a => a.CustomerId == row.Id));
            }
        }

        public override string GetCellControl(string columnId, string cellClass, string boundProperty, Func<string, string> loadTemplate)
        {
            return loadTemplate("TagsColumnTemplate");
        }

        protected override Func<DistributorGridRowModel, IComparable> GetSorter()
        {
            throw new NotSupportedException();
        }
    }
}
