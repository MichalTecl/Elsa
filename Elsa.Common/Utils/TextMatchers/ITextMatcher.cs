using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common.Utils.TextMatchers
{
    public interface ITextMatcher
    {
        bool Match(string text);
    }
}
