using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Apps.ProductionService.Models
{
    public class RequestValidationMessage
    {
        public RequestValidationMessage(bool isError, string text)
        {
            IsError = isError;
            Text = text;
        }

        public bool IsError { get; }

        public string Text { get; }
    }
}
