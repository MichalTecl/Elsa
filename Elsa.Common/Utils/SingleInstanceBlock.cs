using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Common.Utils
{
    public class SingleInstanceBlock
    {
        private static Mutex s_mutex = null;

        public static void EnsureSignleInstance(string mutname) 
        {
            if (s_mutex != null)
            {
                throw new InvalidOperationException("Mutex already created");
            }

            s_mutex = new Mutex(true, mutname, out var thisIsTheSingleInstance);
            if (!thisIsTheSingleInstance)
            {
                Console.WriteLine("Another instance of the application is already running");
                return;
            }
        }
    }
}
