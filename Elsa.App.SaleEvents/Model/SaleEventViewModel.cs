using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.SaleEvents.Model
{
    public class SaleEventViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Date { get; set; }

        public string User { get; set; }

        public string DownloadLink { get; set; }
    }
}
