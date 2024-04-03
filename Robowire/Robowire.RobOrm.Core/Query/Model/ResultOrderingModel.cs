using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

using Robowire.RobOrm.Core.Internal;

namespace Robowire.RobOrm.Core.Query.Model
{
    public class ResultOrderingModel
    {
        public ResultOrderingModel(bool @ascending, Expression expression)
        {
            Ascending = @ascending;
            Expression = expression;
        }

        public bool Ascending { get; }

        public Expression Expression { get; }

        public static void Render<T>(IEnumerable<ResultOrderingModel> orderBy, StringBuilder sb)
        {
            var olist = orderBy?.ToList();
            if ((olist == null) || (olist.Count == 0))
            {
                return;
            }

            sb.Append(" ORDER BY ");

            for (var i = 0; i < olist.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                var order = olist[i];

                var propchain = ExpressionsHelper.GetPropertiesChainText<T>(ExpressionsHelper.RemoveConvert(order.Expression));
                if (string.IsNullOrWhiteSpace(propchain))
                {
                    throw new InvalidOperationException($"Invalid OrderBy expression {order.Expression}");
                }

                var tabAndCol = DotPathHelper.SplitToEntityAndAttribute(propchain);

                sb.Append($"[{tabAndCol.Item1}].[{tabAndCol.Item2}]");

                sb.Append(order.Ascending ? " ASC" : " DESC");
            }

            sb.AppendLine();
        }
    }
}
