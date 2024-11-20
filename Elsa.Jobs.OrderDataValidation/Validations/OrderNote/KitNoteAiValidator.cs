using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Model;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Integration.ChatGpt;
using Elsa.Integration.ChatGpt.Model;
using Elsa.Jobs.OrderDataValidation.Validations.OrderNote.Entities;
using Elsa.Jobs.OrderDataValidation.Validations.OrderNote.Model;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using Robowire.RobOrm.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Elsa.Jobs.OrderDataValidation.Validations.OrderNote
{
    public class KitNoteAiValidator
    {
        private readonly ILog _log;
        private readonly ISession _session;
        private readonly IDatabase _database;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly IKitProductRepository _kitProductRepository;
        private readonly IChatGptClient _gpt;

        public KitNoteAiValidator(ILog log, ISession session, IDatabase database, IPurchaseOrderRepository purchaseOrderRepository, IKitProductRepository kitProductRepository, IChatGptClient gpt)
        {
            _log = log;
            _session = session;
            _database = database;
            _purchaseOrderRepository = purchaseOrderRepository;
            _kitProductRepository = kitProductRepository;
            _gpt = gpt;
        }

        public void Validate()
        {
            _log.Info($"Starting {nameof(KitNoteAiValidator)}");

            var currentValidations = _database
                .Sql()
                .Call("GetKitNoteValidationHistory")
                .WithParam("@projectId", _session.Project.Id)
                .AutoMap<KitNoteValidationHistory>();

            _log.Info($"{currentValidations.Count} records to validate");

            if (currentValidations.Count == 0)
                return;

            foreach (var toValidate in currentValidations.GroupBy(o => o.OrderId))
            {
                var order = _purchaseOrderRepository.GetOrder(toValidate.Key);

                if (toValidate.First().CustomerNoteHash != null)
                {
                    var noteHash = GetNoteHash(order.CustomerNote);
                    if (noteHash == toValidate.First().CustomerNoteHash)
                        continue;
                }

                _log.Info($"Validating kit note for orderNr '{order.OrderNumber}', Note='{order.CustomerName}'");

                if (string.IsNullOrWhiteSpace(order.CustomerNote))
                {
                    SaveValidation(order, "Poznámka je prázdná");
                    continue;
                }

                var parsed = _kitProductRepository.ParseKitNotes(toValidate.Key);
                if (parsed != null)
                {
                    if (TryValidateExact(order, toValidate, parsed))
                        continue;
                }

                ValidateAi(order, toValidate, parsed);
            }
        }

        private void ValidateAi(IPurchaseOrder order, IGrouping<long, KitNoteValidationHistory> toValidate, List<KitNoteParseResultModel> parsed)
        {
            _log.Info($"Starting AI validation of kit note for OrderNr:{order.OrderNumber}");
            var kits = new List<Kit>();

            foreach (var toVal in toValidate)
            {
                var kitDefinition = _kitProductRepository.GetKitDefinition(toVal.KitDefinitionId);
                kits.Add(new Kit
                {
                    Name = kitDefinition.ItemName,
                    Quantity = toVal.Quantity,
                    Groups = kitDefinition.SelectionGroups.Select(sg => new Group
                    {
                        Name = sg.InTextMarker,
                        Options = sg.Items.Select(i => i.InTextMarker).ToList()
                    }).ToList()
                });
            }
                        
            var kitsJson = JsonConvert.SerializeObject(kits);

            var promptSb = new StringBuilder();
            promptSb.Append("Jsi asistent pro balení balíčků. Zákazník nám poslal tuto poznámku: \"")
                .Append(order.CustomerNote)
                .AppendLine("\"")
                .AppendLine()
                .AppendLine("Je potřeba, abys zkontroloval, zda podle poznámky chápeme, co si zákazník vybral do:");

            // Iterace přes všechny sady k validaci
            foreach (var toVal in toValidate)
            {
                var kit = _kitProductRepository.GetKitDefinition(toVal.KitDefinitionId);

                // Iterace přes množství (pokud Quantity > 1)
                for (var nr = 0; nr < toVal.Quantity; nr++)
                {
                    promptSb.Append("  ");

                    // Pokud Quantity je větší než 1, přidáváme pořadí sady
                    if (toVal.Quantity > 1)
                    {
                        promptSb.Append(nr + 1).Append(". ");
                    }

                    // Přidání názvu sady
                    promptSb.AppendLine($"sady s názvem '{kit.ItemName}'. Tato sada má tyto skupiny výběru: ");

                    // Iterace přes skupiny výběru
                    foreach (var grp in kit.SelectionGroups.Where(g => !string.IsNullOrWhiteSpace(g.InTextMarker)))
                    {
                        promptSb.Append("   * Skupina '").Append(grp.InTextMarker)
                            .Append("' : [")
                            .Append(string.Join(", ", grp.Items.Where(i => !string.IsNullOrWhiteSpace(i.InTextMarker)).Select(i => $"'{i.InTextMarker}'")))
                            .AppendLine("]");
                    }

                    // Přidání závěrečného pokynu k určení, zda je poznámka jasná
                    promptSb.AppendLine()
                        .AppendLine(@"Aby byla sada kompletní, zákazník musí jednoznačně uvést, co si přeje z každé skupiny. Pokud poznámka neobsahuje zmínky o konkrétních produktech z těchto skupin, nemůžeš odpovědět 'OK'.

Pokud poznámka **jednoznačně specifikuje** výběr ze všech skupin, odpověz **jen 'OK'** a nic jiného.

Pokud není možné podle poznámky určit, co si zákazník přeje, vysvětli stručně a česky, co chybí nebo není jasné.

Například: pokud poznámka neobsahuje žádný konkrétní produkt z daných skupin, neodpovídej 'OK' a vysvětli proč.");
                }
            }

            var prompt = promptSb.ToString();
            _log.Info($"Requesting AI prompt: {prompt}");

            var request = CreateValidationRequest(prompt);

            try
            {
                var response = _gpt.Request(request);
                var result = response.Choices[0].Message.Content;

                if (result.Equals("OK"))
                {
                    _log.Info("Validation OK");
                    SaveValidation(order, null);
                    return;
                }

                _log.Info($"Validation failure: {result}");
                SaveValidation(order, result);
            }
            catch (Exception ex)
            {
                _log.Error("AI based validation failed", ex);
            }
        }

        public OpenAiRequestBody CreateValidationRequest(string prompt)
        {            
            return new OpenAiRequestBody
            {
                Messages = new List<OpenAiRequestBody.Message>
        {
            new OpenAiRequestBody.Message { Role = "user", Content = prompt }
        },
                Temperature = 0.2f,  // Nízká teplota pro konzistentní a přesné odpovědi
                MaxTokens = 250  // Trochu vyšší limit pro případné chyby
            };
        }



        private bool TryValidateExact(IPurchaseOrder order, IEnumerable<KitNoteValidationHistory> toValidate, List<KitNoteParseResultModel> parsed)
        {
            foreach (var rowToValidate in toValidate)
            {
                var kit = _kitProductRepository.GetKitDefinition(rowToValidate.KitDefinitionId);
                if (kit == null)
                {
                    SaveValidation(order, $"Internal Error - invalid KitId {rowToValidate.KitDefinitionId}");
                    return true;
                }

                for (var kitNr = 0; kitNr < rowToValidate.Quantity; kitNr++)
                    foreach (var selectionGroup in kit.SelectionGroups)
                    {
                        var parsedRow = parsed.FirstOrDefault(p => p.KitDefinitionId == kit.Id
                                                                && p.SelectionGroupId == selectionGroup.Id
                                                                && p.SelectionGroupItemId != null
                                                                && p.KitNr == (kitNr + 1));
                        if (parsedRow == null)
                        {
                            SaveValidation(order, "Text je nekompletní");
                            return false;
                        }

                        parsed.Remove(parsedRow);
                    }
            }

            if (parsed.Count > 0)
            {
                SaveValidation(order, "Počet sad v poznámce neodpovídá počtu sad v objednávce");
                return false;
            }

            SaveValidation(order, null);

            return true;
        }

        private void SaveValidation(IPurchaseOrder order, string message)
        {
            _log.Info($"Saving Kit Note validation result. OrderNr:{order.OrderNumber}, IsValid:{message == null}, Message:{message}");

            var record = _database.New<IKitNoteValidationResult>();
            record.ValidationDt = DateTime.Now;
            record.IsValid = message == null;
            record.ValidatonMessage = message;
            record.PurchaseOrderId = order.Id;
            record.OrderHash = order.OrderHash;
            record.CustomerNoteHash = GetNoteHash(order.CustomerNote);

            _database.Save(record);
        }

        private static string GetNoteHash(string note)
        {
            if (string.IsNullOrWhiteSpace(note))
                return "-1";

            using (var md5 = new MD5CryptoServiceProvider())
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(note));
                return Convert.ToBase64String(hash);
            }
        }

        #region JsonModel
        private class Group
        {
            public string Name { get; set; }
            public List<string> Options { get; set; }
        }

        private class Kit
        {
            public string Name { get; set; }
            public List<Group> Groups { get; set; }
            public int Quantity { get; set; }
        }

        #endregion

    }
}
