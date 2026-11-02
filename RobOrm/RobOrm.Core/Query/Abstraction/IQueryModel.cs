using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using RobOrm.Core.Query.Model;

namespace RobOrm.Core.Query.Abstraction
{
    public interface IQueryModel<T>
    {
        string RootTableAlias { get; }

        string RootTableName { get; }

        int? Take { get; }

        int? Skip { get; }

        IEnumerable<SelectedColumnModel> SelectedColumns { get; }

        IEnumerable<JoinModel> Joins { get; }

        Expression<Func<T, bool>>  Where { get; }

        IEnumerable<ResultOrderingModel> OrderBy { get; }
    }
}
