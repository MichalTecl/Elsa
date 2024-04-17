using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XlsSerializer.Core.Attributes;

namespace Elsa.App.Inspector.ImpExpModules
{
    
    [HeaderStyle(FontStyle = FontStyle.Bold)]
    public class ResponsibilityMatrixRowModel
    {
        [XlsColumn("A", "Typ inspekce")]
        public string IssueTypeName { get; set; }

        [XlsColumn("B", "Uživatel")]
        public string UserName { get; set; }

        [XlsColumn("C", "Počet dnů od nalezení")]
        public int Days { get; set; }

        [XlsColumn("D", "Přeposlat na adresu")]
        public string MailAlias { get; set; }
    }
}
