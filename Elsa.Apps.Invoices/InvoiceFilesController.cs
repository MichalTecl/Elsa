using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Routing;
using Elsa.Apps.Invoices.Model;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Robowire.RoboApi;
using XlsSerializer.Core;

namespace Elsa.Apps.Invoices
{
    [Controller("InvoiceFiles")]
    public class InvoiceFilesController : ElsaControllerBase
    {
        private readonly InvoiceModelFactory m_invoiceModelFactory;
        private readonly IInvoiceFileProcessor m_invoiceFileProcessor;

        public InvoiceFilesController(IWebSession webSession, ILog log, InvoiceModelFactory invoiceModelFactory, IInvoiceFileProcessor invoiceFileProcessor) : base(webSession, log)
        {
            m_invoiceModelFactory = invoiceModelFactory;
            m_invoiceFileProcessor = invoiceFileProcessor;
        }

        public FileResult GetTemplate()
        {
            return new FileResult("sablona_faktura.xlsx", XlsxSerializer.Instance.Serialize(m_invoiceModelFactory.Create()));
        }

        public void UploadInvoiceFile(RequestContext context)
        {
            var file = context.HttpContext.Request.Files[0];
            
            var deserializedModel = XlsxSerializer.Instance.Deserialize<InvoiceModel>(file.InputStream);

            m_invoiceFileProcessor.ProcessFile(deserializedModel);
        }

    }
}
