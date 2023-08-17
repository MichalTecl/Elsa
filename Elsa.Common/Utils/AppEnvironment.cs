using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Common.Utils
{
    public static class AppEnvironment
    {
        public static bool IsDev
        {
            get 
            {
                return File.Exists("C:\\Elsa\\Dev.env");
            }
        }
    }
}
