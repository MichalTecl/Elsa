using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.App.EshopExtensions.Entities;
using Elsa.App.EshopExtensions.Model;
using Elsa.Common.Caching;
using Elsa.Common.Data;
using Elsa.Common.Interfaces;

using Newtonsoft.Json;

using Robowire.RobOrm.Core;

namespace Elsa.App.EshopExtensions.Internal
{
    public class EshopExtensionsRepository : IEshopExtensionsRepository
    {
        private readonly AutoRepo<ICouponValidationRule> _couponRules;
        private readonly IWebSession _session;
        private readonly IDatabase _database;

        public EshopExtensionsRepository(IWebSession session, IDatabase database, ICache cache)
        {
            _session = session;
            _database = database;
            _couponRules = new AutoRepo<ICouponValidationRule>(session, database, cache);
        }

        public EshopExtensionsStatus GetStatus()
        {
            return new EshopExtensionsStatus
            {
                ProjectId = _session.Project.Id,
                DatabaseConnected = _database != null
            };
        }

        public List<CouponValidationRuleListItemModel> GetCouponRules()
        {
            return _couponRules.GetAll()
                .OrderByDescending(r => r.ValidFrom ?? DateTime.MinValue)
                .ThenBy(r => r.RuleName)
                .Select(ToListItem)
                .ToList();
        }

        public CouponValidationRuleEditorModel GetCouponRule(int? ruleId)
        {
            if (ruleId == null)
            {
                return CreateNewRuleModel();
            }

            return ToEditorModel(FindRule(ruleId.Value));
        }

        public CouponValidationRuleEditorModel SaveCouponRule(CouponValidationRuleEditorModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var normalizedModel = NormalizeModel(model);
            var validationError = ValidateForActivation(normalizedModel);
            var canBeActive = string.IsNullOrWhiteSpace(validationError);
            var shouldBeActive = normalizedModel.IsActive && canBeActive;

            var saved = _couponRules.Upsert(normalizedModel.Id, entity =>
            {
                var today = DateTime.Today;

                if (entity.Id < 1)
                {
                    entity.AuthorId = _session.User.Id;
                }

                entity.RuleName = normalizedModel.RuleName ?? string.Empty;
                entity.ValidFrom = shouldBeActive ? today : today.AddDays(-1);
                entity.ValidTo = shouldBeActive ? (DateTime?)null : today.AddDays(-1);
                entity.RuleJson = JsonConvert.SerializeObject(new CouponValidationRule
                {
                    CouponCodes = normalizedModel.CouponCodes,
                    Rules = normalizedModel.Rules
                });
            });

            var result = ToEditorModel(saved);
            result.ValidationMessage = validationError;
            return result;
        }

        public void DeleteCouponRule(int ruleId)
        {
            var entity = FindRule(ruleId);
            _database.Delete(entity);
            _couponRules.ClearCache();
        }

        private ICouponValidationRule FindRule(int id)
        {
            var entity = _couponRules.GetAll().FirstOrDefault(r => r.Id == id);

            if (entity == null)
            {
                throw new InvalidOperationException($"Pravidlo s Id={id} neexistuje");
            }

            return entity;
        }

        private CouponValidationRuleEditorModel CreateNewRuleModel()
        {
            return new CouponValidationRuleEditorModel
            {
                IsActive = true,
                CouponCodes = new List<string>(),
                Rules = new List<Rule> { CreateDefaultRule() },
                ValidationMessage = null
            };
        }

        private CouponValidationRuleEditorModel ToEditorModel(ICouponValidationRule entity)
        {
            var definition = DeserializeRuleDefinition(entity.RuleJson);

            return new CouponValidationRuleEditorModel
            {
                Id = entity.Id,
                RuleName = entity.RuleName,
                IsActive = IsRuleActive(entity),
                ValidationMessage = null,
                CouponCodes = definition.CouponCodes ?? new List<string>(),
                Rules = definition.Rules?.Select(CloneRule).ToList() ?? new List<Rule>()
            };
        }

        private CouponValidationRuleListItemModel ToListItem(ICouponValidationRule entity)
        {
            var definition = DeserializeRuleDefinition(entity.RuleJson);
            var codes = definition.CouponCodes ?? new List<string>();
            var isActive = IsRuleActive(entity);

            return new CouponValidationRuleListItemModel
            {
                Id = entity.Id,
                RuleName = entity.RuleName,
                IsActive = isActive,
                StatusText = isActive ? "Aktivní" : "Neaktivní",
                CouponCodesPreview = string.Join(", ", codes.Take(3)) + (codes.Count > 3 ? "..." : string.Empty),
                CouponCodesCount = codes.Count
            };
        }

        private bool IsRuleActive(ICouponValidationRule entity)
        {
            if (entity.ValidFrom == null)
            {
                return false;
            }

            var now = DateTime.Now;
            return entity.ValidFrom <= now && (entity.ValidTo == null || entity.ValidTo >= now);
        }

        private CouponValidationRule DeserializeRuleDefinition(string ruleJson)
        {
            if (string.IsNullOrWhiteSpace(ruleJson))
            {
                return new CouponValidationRule
                {
                    CouponCodes = new List<string>(),
                    Rules = new List<Rule>()
                };
            }

            return JsonConvert.DeserializeObject<CouponValidationRule>(ruleJson) ?? new CouponValidationRule
            {
                CouponCodes = new List<string>(),
                Rules = new List<Rule>()
            };
        }

        private CouponValidationRuleEditorModel NormalizeModel(CouponValidationRuleEditorModel source)
        {
            return new CouponValidationRuleEditorModel
            {
                Id = source.Id,
                RuleName = source.RuleName?.Trim() ?? string.Empty,
                IsActive = source.IsActive,
                ValidationMessage = null,
                CouponCodes = (source.CouponCodes ?? new List<string>())
                    .Select(c => c?.Trim())
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                Rules = (source.Rules ?? new List<Rule>())
                    .Select(NormalizeRule)
                    .ToList()
            };
        }

        private string ValidateForActivation(CouponValidationRuleEditorModel model)
        {
            if (string.IsNullOrWhiteSpace(model.RuleName))
            {
                return "Pravidlo musí mít název.";
            }

            var duplicateExists = _couponRules.GetAll().Any(r =>
                r.Id != model.Id &&
                string.Equals(r.RuleName?.Trim(), model.RuleName, StringComparison.OrdinalIgnoreCase));

            if (duplicateExists)
            {
                return "Název pravidla musí být jedinečný.";
            }

            if (model.CouponCodes == null || model.CouponCodes.Count == 0)
            {
                return "Pravidlo musí mít alespoň jeden kód.";
            }

            if (model.Rules == null || model.Rules.Count == 0)
            {
                return "Pravidlo musí mít alespoň jednu podmínku.";
            }

            if (model.Rules.Any(HasInvalidRule))
            {
                return "Každá podmínka musí mít produkt a hlášku při nesplnění.";
            }

            return null;
        }

        private bool HasInvalidRule(Rule rule)
        {
            if (rule == null)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(rule.MustHaveProductInCart) ||
                string.IsNullOrWhiteSpace(rule.ViolationMessage))
            {
                return true;
            }

            return rule.AndAlso != null && HasInvalidRule(rule.AndAlso);
        }

        private Rule NormalizeRule(Rule source)
        {
            if (source == null)
            {
                return CreateDefaultRule();
            }

            return new Rule
            {
                MustHaveProductInCart = NormalizeProductLink(source.MustHaveProductInCart),
                MinQuantity = source.MinQuantity <= 0 ? 1 : source.MinQuantity,
                MaxQuantity = source.MaxQuantity <= 0 ? 9999 : source.MaxQuantity,
                ViolationMessage = source.ViolationMessage?.Trim(),
                AndAlso = NormalizeNestedRule(source.AndAlso)
            };
        }

        private Rule NormalizeNestedRule(Rule source)
        {
            if (source == null)
            {
                return null;
            }

            return new Rule
            {
                MustHaveProductInCart = NormalizeProductLink(source.MustHaveProductInCart),
                MinQuantity = source.MinQuantity <= 0 ? 1 : source.MinQuantity,
                MaxQuantity = source.MaxQuantity <= 0 ? 9999 : source.MaxQuantity,
                ViolationMessage = source.ViolationMessage?.Trim(),
                AndAlso = NormalizeNestedRule(source.AndAlso)
            };
        }

        private Rule CloneRule(Rule source)
        {
            if (source == null)
            {
                return null;
            }

            return new Rule
            {
                MustHaveProductInCart = source.MustHaveProductInCart,
                MinQuantity = source.MinQuantity,
                MaxQuantity = source.MaxQuantity,
                ViolationMessage = source.ViolationMessage,
                AndAlso = CloneRule(source.AndAlso)
            };
        }

        private Rule CreateDefaultRule()
        {
            return new Rule
            {
                MinQuantity = 1,
                MaxQuantity = 9999
            };
        }

        private string NormalizeProductLink(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value?.Trim();
            }

            var trimmed = value.Trim();
            var productPathStart = trimmed.IndexOf("/p/", StringComparison.OrdinalIgnoreCase);

            if (productPathStart >= 0)
            {
                trimmed = trimmed.Substring(productPathStart);
            }

            var queryIndex = trimmed.IndexOfAny(new[] { '?', '#' });
            if (queryIndex >= 0)
            {
                trimmed = trimmed.Substring(0, queryIndex);
            }

            return trimmed;
        }
    }
}
