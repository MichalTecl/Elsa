using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Model;
using Elsa.Common.Caching;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Extensions;
using Elsa.Core.Entities.Commerce.Inventory.Batches;
using Elsa.Core.Entities.Commerce.Inventory.Kits;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class KitProductRepository : IKitProductRepository
    {
        private const string _cacheKey = "completeKitProductDefinitions";
        private const string _itemKitsIndexCacheKey = "kitItem_kits";

        private readonly IPerProjectDbCache _cache;
        private readonly IDatabase _database;
        private readonly IPurchaseOrderRepository _orderRepository;
        private readonly ISession _session;
        private readonly ILog _logger;

        public KitProductRepository(IPerProjectDbCache cache, IDatabase database, IPurchaseOrderRepository orderRepository, ISession session, ILog logger)
        {
            _cache = cache;
            _database = database;
            _orderRepository = orderRepository;
            _session = session;
            _logger = logger;
        }

        public IEnumerable<IKitDefinition> GetAllKitDefinitions()
        {
            return _cache.ReadThrough(_cacheKey,
                db =>
                    {
                        return
                            db.SelectFrom<IKitDefinition>()
                                .Join(k => k.SelectionGroups)
                                .Join(k => k.SelectionGroups.Each().Items);
                    });
        }

        public IEnumerable<KitItemsCollection> GetKitForOrderItem(IPurchaseOrder order, IOrderItem item)
        {
            var result = new List<KitItemsCollection>();

            var matchingDefinitions = GetAllKitDefinitions().Where(k => k.IsMatch(order, item)).OrderBy(k => k.Id).ToList();
            if (matchingDefinitions.Count == 0)
            {
                return result;
            }

            var selectedChildItems = _orderRepository.GetChildItemsByParentItemId(item.Id).OrderBy(i => i.Id).ToList();

            for (var kitItemIndex = 0; kitItemIndex < (int)item.Quantity; kitItemIndex++)
            {
                foreach (var kitDefinition in matchingDefinitions.OrderBy(md => md.Id))
                {
                    foreach (var selection in kitDefinition.SelectionGroups.OrderBy(sg => sg.Id))
                    {
                        IOrderItem selectedItem = null;
                        foreach (var selectionItem in selection.Items.OrderBy(i => i.Id))
                        {
                            selectedItem = selectedChildItems.FirstOrDefault(i => selectionItem.IsMatch(null, i) && (i.KitItemIndex == kitItemIndex));
                            if (selectedItem != null)
                            {
                                selectedChildItems.Remove(selectedItem);
                                break;
                            }
                        }

                        result.Add(new KitItemsCollection(kitDefinition.Id, selection.Items, selectedItem, kitItemIndex, selection.Id, selection.Name));
                    }
                }
            }

            return result.OrderBy(r => r.GroupId);
        }

        public IEnumerable<KitItemsCollection> SetKitItemSelection(IPurchaseOrder order, IOrderItem item, int kitItemId, int kitItemIndex)
        {
            try
            {
                var currentKit = GetKitForOrderItem(order, item).ToList();
                if (!currentKit.Any())
                {
                    throw new InvalidOperationException("Zvoleny produkt neni sada");
                }

                var found = false;
                
                foreach (var group in currentKit.Where(g => g.KitItemIndex == kitItemIndex))
                {
                    var targetKitItem = group.GroupItems.FirstOrDefault(i => i.Id == kitItemId);
                    if (targetKitItem == null)
                    {
                        continue;
                    }

                    found = true;

                    using (var tx = _database.OpenTransaction())
                    {
                        if (group.SelectedItem != null)
                        {
                            var assignmentsToDelete = _database.SelectFrom<IOrderItemMaterialBatch>()
                                .Where(a => a.OrderItemId == group.SelectedItem.Id).Execute();

                            _database.DeleteAll(assignmentsToDelete);

                            _database.Delete(group.SelectedItem);
                        }

                        var orderItem = _database.New<IOrderItem>();
                        orderItem.ErpProductId = targetKitItem.ErpProductId;
                        orderItem.KitParentId = item.Id;
                        orderItem.PlacedName = targetKitItem.ItemName;
                        orderItem.Quantity = 1;
                        orderItem.KitItemIndex = kitItemIndex;

                        _database.Save(orderItem);

                        tx.Commit();
                        break;
                    }
                }

                if (!found)
                {
                    throw new InvalidOperationException("Nezdarilo se prirazeni produktu");
                }
            }
            finally
            {
                _cache.Remove(_cacheKey);
            }

            return GetKitForOrderItem(order, item);
        }

        public bool IsKit(IPurchaseOrder order, IOrderItem item)
        {
            return GetAllKitDefinitions().FirstOrDefault(k => k.IsMatch(order, item)) != null;
        }

        public List<KitNoteParseResultModel> ParseKitNotes(long? orderId)
        {
            return _cache.ReadThrough($"kitNoteParseResult_{orderId}", 
                TimeSpan.FromMinutes(1),
                () =>
                {
                    try
                    {
                        return _database.Sql()
                            .Call("ParseKitNote")
                            .WithParam("@orderId", orderId)
                            .WithParam("@projectId", _session.Project.Id)
                            .AutoMap<KitNoteParseResultModel>();
                    }
                    catch(Exception e)
                    {
                        _logger.Error($"Error parsing kit note for orderId={orderId}", e);
                        return new List<KitNoteParseResultModel>(0);
                    }
                });
        }

        public IKitDefinition GetKitDefinition(int id)
        {
            return GetAllKitDefinitions().FirstOrDefault(k => k.Id == id);
        }

        private static readonly ICollection<IKitDefinition> _emptyKdList = new List<IKitDefinition>(0).AsReadOnly();

        public ICollection<IKitDefinition> GetKitsByItemName(string itemName)
        {
            var kindex = _cache.ReadThrough(_itemKitsIndexCacheKey, () => {

                var allKits = GetAllKitDefinitions();

                var index = new Dictionary<string, List<IKitDefinition>>();

                foreach (var kitDefinition in allKits) 
                    foreach(var selectionGroup in kitDefinition.SelectionGroups)
                        foreach(var groupItem in selectionGroup.Items)
                        {
                            if(!index.TryGetValue(groupItem.ItemName, out var relatedKits))
                            {
                                relatedKits = new List<IKitDefinition>();
                                index[groupItem.ItemName] = relatedKits;
                            }
                            else if (relatedKits.Any(rk => rk.Id == kitDefinition.Id))
                            {
                                continue;
                            }

                            relatedKits.Add(kitDefinition);
                        }

                return index;            
            });

            if (!kindex.TryGetValue(itemName, out var kits))
                return _emptyKdList;

            return kits;
        }

        public void UpdateKitItemMapping(int kitItemId, string newItemName)
        {
            newItemName = newItemName?.Trim();

            if (string.IsNullOrWhiteSpace(newItemName))
                throw new ArgumentNullException("Název položky nesmí být prázdný");

            if(newItemName.Length > 255)
                throw new ArgumentException("Název položky smí mít nejvíce 255 znaků");

            var all = GetAllKitDefinitions();

            IKitSelectionGroupItem item = null;

            foreach(var kd in all)
                foreach(var sg in kd.SelectionGroups)
                    foreach(var i in sg.Items)
                    {
                        if (i.Id == kitItemId)
                        {
                            item = i;
                            break;
                        }
                    }

            item.ErpProductId = null;
            item.ItemName = newItemName;
            _database.Save(item);

            ClearCache();
        }

        private void ClearCache()
        {
            _cache.Remove(_cacheKey);
            _cache.Remove(_itemKitsIndexCacheKey);
        }
    }
}
