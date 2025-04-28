using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Common.DbUtils
{
    public interface IProcedureLister
    {
        List<ProcedureInfo> ListProcedures(string likeNamePattern);
    }
}
