using System.Collections.Generic;
using Elsa.Apps.InvoiceForms.Facade;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Robowire.RoboApi;

namespace Elsa.Apps.InvoiceForms
{
    [Controller("invoiceFormPackages")]
    public class InvoiceFormPackagesController : ElsaControllerBase
    {
        private readonly InvoiceFormsManagementFacade _facade;

        public InvoiceFormPackagesController(IWebSession webSession, ILog log, InvoiceFormsManagementFacade facade)
            : base(webSession, log)
        {
            _facade = facade;
        }

        public List<InvoiceFormsManagementFacade.InvoiceFormCollectionViewModel> GetCollections()
        {
            return _facade.GetCollections();
        }

        public List<InvoiceFormsManagementFacade.InvoiceFormCollectionViewModel> DeletePackage(string publicUid)
        {
            _facade.DeletePackage(publicUid);
            return _facade.GetCollections();
        }
    }
}