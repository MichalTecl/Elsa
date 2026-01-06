using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using Elsa.Commerce.Core;
using Elsa.Commerce.Core.Shipment;
using Elsa.Common.Caching;
using Elsa.Common.Communication;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Common.Utils;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Common;
using Elsa.Core.Entities.Commerce.Integration;
using Elsa.Integration.ShipmentProviders.Zasilkovna.Entities;
using Elsa.Integration.ShipmentProviders.Zasilkovna.Model;
using Robowire.RobOrm.Core;

namespace Elsa.Integration.ShipmentProviders.Zasilkovna
{
    public class ZasilkovnaClient : IShipmentProvider
    {
        private readonly WebFormsClient _formsClient;
                        
        private BranchesDocument _branches;

        private readonly ILog _log;
        private readonly ZasilkovnaClientConfig _config;
        private readonly IDatabase _database;
        private readonly ICache _cache;
        private readonly ISession _session;

        public ZasilkovnaClient(ILog log, ZasilkovnaClientConfig config, IDatabase database, ICache cache, ISession session)
        {
            _log = log;
            _config = config;
            _database = database;
            _cache = cache;
            _session = session;
            _formsClient = new WebFormsClient(log);
        }
        
        internal string GetPobockaId(string deliveryName, IDictionary<string, string> shipmentMethodsMapping) 
        {
            var originalDeliveryName = deliveryName;

            deliveryName = deliveryName.Trim();

            while (true) 
            {
                string mapped;
                if (!shipmentMethodsMapping.TryGetValue(deliveryName, out mapped))
                    break;

                if (deliveryName == mapped) 
                {
                    _log.Error($"Shipment method mapping loop '{deliveryName}'");
                    break;
                }

                _log.Info($"Appplied shipment method mapping: '{deliveryName}' -> '{mapped}'");
                deliveryName = mapped;
            }

            var index = GetShipMethodIdIndex();
            
            var key = deliveryName.ToLowerInvariant().Trim();
            if (!index.TryGetValue(key, out var id)) 
            {
                _log.Error($"Delivery name was '{originalDeliveryName}', mapped by rules to '{deliveryName}'. Not found in the index by '{key}'. Index.Count={index.Count}");

                throw new Exception(string.Format("Neexistující způsob dopravy \"{0}\"", originalDeliveryName));
            }

            return id;
        }

        private Dictionary<string, string> GetShipMethodIdIndex() 
        {
            return _cache.ReadThrough("zasilkovnaShipMethodIndex", TimeSpan.FromSeconds(10), () => {

                var fromDatabase = _database.SelectFrom<IZasilkovnaShipMapping>().Execute();
                var result = new Dictionary<string, string>();

                foreach (var dbRecord in fromDatabase)
                    result[dbRecord.Name.ToLowerInvariant().Trim()] = dbRecord.ZasilkovnaId;

                var goldSrc = GetBranchIndexFromZasilkovna();

                foreach(var srcRec in goldSrc)
                {
                    if(result.TryGetValue(srcRec.Item1, out var idInDatabase) && (idInDatabase == srcRec.Item2)) 
                    {
                        continue;
                    }

                    SaveShipMethod(srcRec);
                    result[srcRec.Item1] = srcRec.Item2;
                }

                return result;
            });
        }

        private void SaveShipMethod(Tuple<string, string> srcRec)
        {
            var matchingRecs = _database.SelectFrom<IZasilkovnaShipMapping>().Where(m => m.Name == srcRec.Item1).Execute().ToList();
            foreach(var mrec in matchingRecs) 
            {
                _log.Error($"WARN - Detected shipmentId change - '{mrec.Name}' [{mrec.ZasilkovnaId}] -> [{srcRec.Item1}]");
                _database.Delete(mrec);
            }

            var newMap = _database.New<IZasilkovnaShipMapping>();
            newMap.ZasilkovnaId = srcRec.Item2;
            newMap.Name = srcRec.Item1;

            _database.Save(newMap);

            _log.Info($"Zasilkova Shipment Method index added: {newMap.ZasilkovnaId} : {newMap.Name}");
        }

        private IEnumerable<Tuple<string, string>> GetBranchIndexFromZasilkovna() 
        {            
            var branches = GetBranches().BranchesList.Branches;
            foreach(var branch in branches) 
            {
                var n = branch.Name.Trim().ToLowerInvariant();
                var l = branch.LabelName.Trim().ToLowerInvariant();

                yield return new Tuple<string, string>($"{n} {branch.Id}", branch.Id);

                yield return new Tuple<string, string>(n, branch.Id);

                if (n != l)
                    yield return new Tuple<string, string>(l, branch.Id);
            }
        }

        public string GetOrderNumberByPackageNumber(string packageNumber)
        {
            const string orderNoHeader = "Číslo objednávky</th>";

            var url = $"https://www.zasilkovna.cz/vyhledavani/{packageNumber}";

            /*
						<th>Číslo objednávky</th>
						<td>1800529</td>
					*/

            try
            {
                var html = _formsClient.GetString(url);

                var thIndex = html.IndexOf(orderNoHeader, StringComparison.Ordinal);
                if (thIndex < 0)
                {
                    return null;
                }

                html = html.Substring(thIndex + orderNoHeader.Length + 1).Trim();
                html = html.Substring(0, 100);
                html = html.Replace("<td>", string.Empty);

                var cIndex = html.IndexOf("</td>", StringComparison.Ordinal);
                html = html.Substring(0, cIndex).Trim();

                return html;
            }
            catch(WebException ex)
            {                
                _log.Error("Chyba při hledání čísla zásilky v Zásilkovně", ex);
                return null;
            }
        }

        public void SetShipmentMethodsMapping(Dictionary<string, string> mapping)
        {
            try
            {
                using (var tx = _database.OpenTransaction())
                {
                    _database.DeleteFrom<IShipmentMethodMapping>(q => q.Where(m => m.ProjectId == _session.Project.Id));

                    foreach (var map in mapping)
                    {
                        var entity = _database.New<IShipmentMethodMapping>();
                        entity.Source = map.Key.Trim();
                        entity.Target = map.Value.Trim();
                        entity.ProjectId = _session.Project.Id;

                        _database.Save(entity);
                    }

                    tx.Commit();
                }
                
            }
            finally
            {
                _cache.Remove($"shipmentMethodsMap_{_session.Project.Id}");
            }
        }

        public Dictionary<string, string> GetShipmentMethodsMapping()
        {
            return _cache.ReadThrough($"shipmentMethodsMap_{_session.Project.Id}", TimeSpan.FromHours(1), () =>
            {
                return _database.SelectFrom<IShipmentMethodMapping>()
                    .Where(m => m.ProjectId == _session.Project.Id).Execute().ToDictionary(m => m.Source, m => m.Target);
            });
        }

        private BranchesDocument GetBranches()
        {
            return _branches ?? (_branches = BranchesDocument.DownloadV5(_log, _config.ApiToken));
        }
                
        public IEnumerable<string> GetShipmentMethodsList()
        {
            var branches = GetBranches();
            foreach(var branch in branches.BranchesList.Branches)
            {
                yield return branch.Name;

                if (branch.Name != branch.LabelName)
                    yield return branch.LabelName;
            }
        }        
    }
}
