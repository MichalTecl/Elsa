using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Jobs.CrmMailPull.Entities;
using Robowire.RoboApi;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa.App.Crm.Controllers
{
    [Controller("CrmMailFilterRules")]
    public class CrmMailFilterRulesController : ElsaControllerBase
    {
        private readonly IDatabase _db;

        public CrmMailFilterRulesController(IWebSession webSession, ILog log, IDatabase db)
            : base(webSession, log)
        {
            _db = db;
        }

        protected override void OnBeforeCall()
        {
            base.OnBeforeCall();
            EnsureUserRight(CrmUserRights.MailPullRulesAdmin);
        }

        public List<AddressRuleVm> GetAddressRules()
        {
            return _db.SelectFrom<IMailPullAddressBlacklist>()
                .OrderByDesc(r => r.Id)
                .Execute()
                .Select(r => new AddressRuleVm
                {
                    Id = r.Id,
                    Pattern = r.Pattern
                })
                .ToList();
        }

        public List<AddressRuleVm> SaveAddressRule(AddressRuleVm model)
        {
            var entity = (model?.Id ?? 0) > 0
                ? _db.SelectFrom<IMailPullAddressBlacklist>().Where(r => r.Id == model.Id).Take(1).Execute().FirstOrDefault()
                : _db.New<IMailPullAddressBlacklist>();

            if (entity == null)
                throw new InvalidOperationException("Pravidlo se nepodařilo najít");

            entity.Pattern = (model?.Pattern ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(entity.Pattern))
                throw new InvalidOperationException("Filtr adresy nesmí být prázdný");

            _db.Save(entity);
            return GetAddressRules();
        }

        public List<AddressRuleVm> DeleteAddressRule(int id)
        {
            var entity = _db.SelectFrom<IMailPullAddressBlacklist>().Where(r => r.Id == id).Take(1).Execute().FirstOrDefault();
            if (entity != null)
                _db.Delete(entity);

            return GetAddressRules();
        }

        public List<ContentRuleVm> GetContentRules()
        {
            return _db.SelectFrom<IMailContentBlacklist>()
                .OrderByDesc(r => r.Id)
                .Execute()
                .Select(r => new ContentRuleVm
                {
                    Id = r.Id,
                    SubjectPattern = r.SubjectPattern,
                    BodyPattern = r.BodyPattern
                })
                .ToList();
        }

        public List<ContentRuleVm> SaveContentRule(ContentRuleVm model)
        {
            var entity = (model?.Id ?? 0) > 0
                ? _db.SelectFrom<IMailContentBlacklist>().Where(r => r.Id == model.Id).Take(1).Execute().FirstOrDefault()
                : _db.New<IMailContentBlacklist>();

            if (entity == null)
                throw new InvalidOperationException("Pravidlo se nepodařilo najít");

            entity.SubjectPattern = (model?.SubjectPattern ?? string.Empty).Trim();
            entity.BodyPattern = (model?.BodyPattern ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(entity.SubjectPattern) && string.IsNullOrWhiteSpace(entity.BodyPattern))
                throw new InvalidOperationException("Musí být vyplněn předmět nebo tělo");

            _db.Save(entity);
            return GetContentRules();
        }

        public List<ContentRuleVm> DeleteContentRule(int id)
        {
            var entity = _db.SelectFrom<IMailContentBlacklist>().Where(r => r.Id == id).Take(1).Execute().FirstOrDefault();
            if (entity != null)
                _db.Delete(entity);

            return GetContentRules();
        }

        public class AddressRuleVm
        {
            public int Id { get; set; }
            public string Pattern { get; set; }
        }

        public class ContentRuleVm
        {
            public int Id { get; set; }
            public string SubjectPattern { get; set; }
            public string BodyPattern { get; set; }
        }
    }
}
