using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common.Logging
{
    public interface ILog
    {
        void Debug(string s);

        void Error(string s, Exception e);

        void Error(string s);
    }
}
