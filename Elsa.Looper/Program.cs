using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Looper
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = ConfigurationManager.AppSettings["processPath"];
            var interval = int.Parse(ConfigurationManager.AppSettings["intervalSeconds"]);
            var timeout = int.Parse(ConfigurationManager.AppSettings["timeoutSeconds"]);

            var lastStartDt = DateTime.MinValue;
            while (true)
            {
                var totalSec = (int)(DateTime.Now - lastStartDt).TotalSeconds;

                lastStartDt = DateTime.Now;

                var toWait = Math.Max(5, interval - totalSec);

                Console.WriteLine($"Waiting {toWait} seconds");

                Thread.Sleep(toWait * 1000);

                try
                {
                    Console.WriteLine($"Starting the process");
                    using (var process = Process.Start(path))
                    {
                        Console.WriteLine($"Started");
                        if (!process.WaitForExit(timeout*1000))
                        {
                            Console.WriteLine("Process execution timed out");
                            process.Kill();
                        }
                        
                        Console.WriteLine("Process finished");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
    }
}
