using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.App.Inspector.Model
{
    public class UserIssuesCount
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int IssuesCount { get; set; }              
    }
}
