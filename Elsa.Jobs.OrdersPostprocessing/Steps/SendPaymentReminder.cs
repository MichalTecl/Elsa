using Elsa.App.Emailing.Internal;
using Elsa.Commerce.Core;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Extensions;
using Elsa.Smtp.Core;
using Robowire.RobOrm.Core;
using System;
using System.Linq;

namespace Elsa.Jobs.OrdersPostprocessing.Steps
{
    /// <summary>
    /// Hledame objednavky, ktere cekaji na platbu dele nez tyden. Zakaznikovi pak posleme upominku e-mailem.
    /// Toto smi probehnout max jednou pro kazdou objednavku.
    /// Nesmi se to dotknout dobirek ani plateb kartou
    /// Pokud zakaznik vytvoril novejsi objednavku, upominka se neposila
    /// </summary>
    public class SendPaymentReminder : ProcessStepBase
    {
        private const string MAIL_TEMPLATE_NAME = "Upomínka platby";

        private readonly IOrderPaymentHelper _orderPaymentHelper;
        private readonly ILog _log;
        private readonly IMailSender _mailSender;
        private readonly IMailTemplateRepository _mailTemplateRepository;
        private readonly IDatabase _database;

        public SendPaymentReminder(
            IDatabase database,
            ISession session,
            ILog log,
            IOrdersFacade ordersFacade,
            IOrderPaymentHelper orderPaymentHelper,
            IMailSender mailSender,
            IMailTemplateRepository mailTemplateRepository)
            : base(database, session, log, ordersFacade)
        {
            _database = database;
            _log = log;
            _orderPaymentHelper = orderPaymentHelper;
            _mailSender = mailSender;
            _mailTemplateRepository = mailTemplateRepository;
        }

        protected override string ProcessCode => OrderProcessingCodes.PAYMENT_REMINDER_SENT;

        protected override int HistoryDepthDays => 100;

        protected override IOrderStatus[] SourceOrderStatuses => new[] { OrderStatus.PendingPayment };

        protected override IQueryBuilder<IPurchaseOrder> QueryOrders(IQueryBuilder<IPurchaseOrder> query)
        {
            var maxDt = DateTime.Now.AddDays(-7);
            
            // jen pro objednavky starsi nez tyden
            query = query.Where(o => o.PurchaseDate < maxDt);

            // ne dobirky
            query = query.Where(o => !o.IsPayOnDelivery);

            return query;
        }

        protected override bool TryProcessOrder(IPurchaseOrder order, Action<string> processingLogMessageWriter)
        {
            if (_orderPaymentHelper.GetPaymentMethodType(order) != PaymentMethodType.BankTransfer)
                return false;

            var newerOrder = _database
                .SelectFrom<IPurchaseOrder>()
                .Where(o => o.ProjectId == order.ProjectId)
                .Where(o => o.CustomerErpUid == order.CustomerErpUid || o.CustomerEmail == order.CustomerEmail)
                .Where(o => o.PurchaseDate > order.PurchaseDate)
                .OrderBy(o => o.Id)
                .Take(1)
                .Execute()
                .FirstOrDefault();

            if (newerOrder != null)
            {
                _log.Info($"Found newer order of the same customer: {newerOrder.OrderNumber} {newerOrder.CustomerName} - reminder will not be sent");
                return false;
            }

            _log.Info($"Found order to send payment reminder: {order.OrderNumber} {order.CustomerName} PurchaseDate = {order.PurchaseDate} ErpStatusName = {order.ErpStatusName}");

            if (!_mailTemplateRepository.Exists(MAIL_TEMPLATE_NAME))
            {
                _log.Info($"E-mail template {MAIL_TEMPLATE_NAME} does not exist - reminder sending is inactive");
                return false;
            }

            var values = order.ToDictionary();

            _mailSender.Send(SenderMailboxType.CustomerFacingSender, order.CustomerEmail, MAIL_TEMPLATE_NAME, values);

            processingLogMessageWriter("Odeslána připomínka platby");
            
            return true;
        }
    }
}
