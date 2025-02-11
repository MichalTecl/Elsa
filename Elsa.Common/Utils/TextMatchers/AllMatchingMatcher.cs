using Elsa.Common.Utils.TextMatchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common.Utils
{
    public class AllMatchingMatcher : ITextMatcher
    {
        public static readonly ITextMatcher Instance = new AllMatchingMatcher();

        public bool Match(string text) => true;        
    }
}
