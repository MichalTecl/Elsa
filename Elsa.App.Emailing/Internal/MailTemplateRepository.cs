using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.App.Emailing.Entities;
using Elsa.App.Emailing.Model;
using Elsa.Common.Caching;
using Elsa.Common.Data;
using Elsa.Common.Interfaces;

using Robowire.RobOrm.Core;

namespace Elsa.App.Emailing.Internal
{
    public class MailTemplateRepository : IMailTemplateRepository
    {
        private const string DELETED_PREFIX = "SMAZÁNO_";

        private readonly AutoRepo<IMailTemplate> _templates;
        private readonly IWebSession _session;

        public MailTemplateRepository(IWebSession session, IDatabase database, ICache cache)
        {
            _session = session;
            _templates = new AutoRepo<IMailTemplate>(session, database, cache);
        }

        public List<MailTemplateModel> GetAll()
        {
            return _templates.GetAll()
                .Where(template => !IsDeleted(template))
                .OrderBy(template => template.TypeName)
                .Select(ToModel)
                .ToList();
        }

        public MailTemplateModel Get(int? id)
        {
            if (id == null)
            {
                return new MailTemplateModel();
            }

            return ToModel(FindActive(id.Value));
        }

        public MailTemplateModel GetByTypeName(string typeName)
        {
            var normalizedTypeName = typeName?.Trim();
            var template = _templates.GetAll().FirstOrDefault(item =>
                !IsDeleted(item)
                && string.Equals(item.TypeName?.Trim(), normalizedTypeName, StringComparison.OrdinalIgnoreCase));

            if (template == null)
            {
                throw new InvalidOperationException($"E-mailová šablona typu '{normalizedTypeName}' neexistuje.");
            }

            return ToModel(template);
        }

        public MailTemplateModel Save(MailTemplateModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var typeName = model.TypeName?.Trim();
            if (string.IsNullOrWhiteSpace(typeName))
            {
                throw new InvalidOperationException("Typ šablony musí být vyplněný.");
            }

            if (typeName.StartsWith(DELETED_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Typ šablony nesmí začínat rezervovaným prefixem {DELETED_PREFIX}.");
            }

            var duplicateExists = _templates.GetAll().Any(template =>
                !IsDeleted(template)
                && template.Id != model.Id
                && string.Equals(template.TypeName?.Trim(), typeName, StringComparison.OrdinalIgnoreCase));

            if (duplicateExists)
            {
                throw new InvalidOperationException($"Šablona typu '{typeName}' již existuje.");
            }

            var saved = _templates.Upsert(model.Id, template =>
            {
                if (template.Id < 1)
                {
                    template.ProjectId = _session.Project.Id;
                }
                else if (IsDeleted(template))
                {
                    throw new InvalidOperationException("Smazanou šablonu nelze upravit.");
                }

                template.TypeName = typeName;
                template.Subject = model.Subject?.Trim() ?? string.Empty;
                template.Body = model.Body ?? string.Empty;
                SetChangeInfo(template);
            });

            return ToModel(saved);
        }

        public void Delete(int id)
        {
            var template = FindActive(id);
            template.TypeName = $"{DELETED_PREFIX}{template.Id}";
            SetChangeInfo(template);
            _templates.Save(template);
        }

        private IMailTemplate FindActive(int id)
        {
            var template = _templates.GetAll().FirstOrDefault(item => item.Id == id);
            if (template == null || IsDeleted(template))
            {
                throw new InvalidOperationException($"E-mailová šablona s Id={id} neexistuje.");
            }

            return template;
        }

        private void SetChangeInfo(IMailTemplate template)
        {
            template.LastChangeDt = DateTime.Now;
            template.LastChangeUserId = _session.User.Id;
        }

        private static bool IsDeleted(IMailTemplate template)
        {
            return template.TypeName?.StartsWith(DELETED_PREFIX, StringComparison.OrdinalIgnoreCase) == true;
        }

        private static MailTemplateModel ToModel(IMailTemplate template)
        {
            return new MailTemplateModel
            {
                Id = template.Id,
                TypeName = template.TypeName,
                Subject = template.Subject,
                Body = template.Body,
                LastChangeDt = template.LastChangeDt,
                LastChangeUserName = template.LastChangeUser?.EMail
            };
        }
    }
}
