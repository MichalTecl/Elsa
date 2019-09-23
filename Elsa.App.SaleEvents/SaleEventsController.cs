using System.Linq;
using System.Web;
using System.Web.Routing;
using Elsa.App.SaleEvents.Model;
using Elsa.App.SaleEvents.Model.Xls;
using Elsa.Commerce.Core.SaleEvents;
using Elsa.Common;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce.SaleEvents;
using Robowire.RoboApi;
using XlsSerializer.Core;

namespace Elsa.App.SaleEvents
{
    [Controller("saleEvents")]
    public class SaleEventsController : ElsaControllerBase
    {
        private readonly ISaleEventRepository m_saleEventRepository;
        private readonly EventModelProcessor m_eventModelProcessor;

        public SaleEventsController(IWebSession webSession, ILog log, ISaleEventRepository saleEventRepository, EventModelProcessor eventModelProcessor) : base(webSession, log)
        {
            m_saleEventRepository = saleEventRepository;
            m_eventModelProcessor = eventModelProcessor;
        }

        public FileResult GetXls(int eventId)
        {
            var model = m_eventModelProcessor.ExportEvent(eventId);

            return new FileResult($"{StringUtil.ToFileName(model.Name)}.xlsx", XlsxSerializer.Instance.Serialize(model));
        }

        public FileResult GetTemplate()
        {
            var model = m_eventModelProcessor.GetSaleEventModelTemplate();

            return new FileResult("prodejniakce.xlsx", XlsxSerializer.Instance.Serialize(model));
        }

        public SaleEventsCollection GetEvents(int pageNumber)
        {
            var evts = m_saleEventRepository.GetEvents(pageNumber, 10).Select(Map).ToList();

            return new SaleEventsCollection(evts.Count == 10 ? pageNumber + 1 : (int?)null, evts);
        }

        public SaleEventViewModel Upload(RequestContext context)
        {
            var file = context.HttpContext.Request.Files[0];

            var deserializedModel = XlsxSerializer.Instance.Deserialize<SaleEventModel>(file.InputStream);

            var e = m_eventModelProcessor.Import(deserializedModel);

            return Map(e);
        }

        private SaleEventViewModel Map(ISaleEvent e)
        {
            return new SaleEventViewModel
            {
                Id = e.Id,
                Date = StringUtil.FormatDate(e.EventDt),
                User = e.User.EMail,
                DownloadLink = $"/saleEvents/getXls?eventId={e.Id}",
                Name = e.Name
            };
        }
    }
}
