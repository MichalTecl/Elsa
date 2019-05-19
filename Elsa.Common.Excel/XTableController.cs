using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Elsa.Common.Logging;
using Elsa.Common.XTable.Generators;
using Elsa.Common.XTable.Model;

using Robowire.RoboApi;

namespace Elsa.Common.XTable
{
    [Controller("xtable")]
    public class XTableController : ElsaControllerBase
    {

        private static ConcurrentDictionary<string, XWorkbook> s_savedBooks = new ConcurrentDictionary<string, XWorkbook>();

        public XTableController(IWebSession webSession, ILog log)
            : base(webSession, log)
        {
        }

        public string UploadWorkbook(XWorkbook workbook)
        {
            if (s_savedBooks.Count > 10)
            {
                throw new InvalidOperationException("OVERLOADED");
            }

            var guid = Guid.NewGuid().ToString();

            s_savedBooks.TryAdd(guid, workbook);

            return guid;
        }

        public FileResult GetExcel(string contentKey, string fileName)
        {
            if (!s_savedBooks.TryRemove(contentKey, out var workbook))
            {
                throw new InvalidOperationException("Invalid key");
            }

            return new FileResult(fileName, XlsGenerator.CreateExcel(workbook), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }
    }
}
