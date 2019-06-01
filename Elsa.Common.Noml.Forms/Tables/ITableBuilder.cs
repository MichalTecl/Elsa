using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common.Noml.Core;

namespace Elsa.Common.Noml.Forms.Tables
{
    public interface ITableBuilder : IRowsBuilder
    {
        IRowsBuilder Head(params string[] names);
    }

    public interface IRowsBuilder : IElement
    {
        IRowsBuilder Rows<T>(IEnumerable<T> collection, Func<T, object[]> generator);

        IRowsBuilder Row(params object[] values);
    }
}
