using System;
using System.Collections.Generic;
using System.Linq;

using Elsa.Commerce.Core.Model;
using Elsa.Common.Caching;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Extensions;
using Elsa.Core.Entities.Commerce.Inventory.Kits;

using Robowire.RobOrm.Core;

namespace Elsa.Commerce.Core.Repositories
{
    public class KitProductRepository : IKitProductRepository
    {
        private const string c_cacheKey = "completeKitProductDefinitions";
        private readonly IPerProjectDbCache m_cache;
        private readonly IDatabase m_database;
        private readonly IPurchaseOrderRepository m_orderRepository;

        public KitProductRepository(IPerProjectDbCache cache, IDatabase database, IPurchaseOrderRepository orderRepository)
        {
            m_cache = cache;
            m_database = database;
            m_orderRepository = orderRepository;
        }

        public IEnumerable<IKitDefinition> GetAllKitDefinitions()
        {
            return m_cache.ReadThrough(c_cacheKey,
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
            var matchingDefinitions = GetAllKitDefinitions().Where(k => k.IsMatch(order, item)).OrderBy(k => k.Id).ToList();
            if (matchingDefinitions.Count == 0)
            {
                yield break;
            }

            var selectedChildItems = m_orderRepository.GetChildItemsByParentItemId(item.Id).OrderBy(i => i.Id).ToList();

            for (var kitItemIndex = 0; kitItemIndex < (int)item.Quantity; kitItemIndex++)
            {
                foreach (var kitDefinition in matchingDefinitions)
                {
                    foreach (var selection in kitDefinition.SelectionGroups)
                    {
                        IOrderItem selectedItem = null;
                        foreach (var selectionItem in selection.Items)
                        {
                            selectedItem = selectedChildItems.FirstOrDefault(i => selectionItem.IsMatch(null, i) && i.KitItemIndex == kitItemIndex);
                            if (selectedItem != null)
                            {
                                selectedChildItems.Remove(selectedItem);
                                break;
                            }
                        }

                        yield return new KitItemsCollection(selection.Items, selectedItem, kitItemIndex, selection.Id, selection.Name);
                    }
                }
            }
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

                    using (var tx = m_database.OpenTransaction())
                    {
                        if (group.SelectedItem != null)
                        {
                            m_database.Delete(group.SelectedItem);
                        }

                        var orderItem = m_database.New<IOrderItem>();
                        orderItem.ErpProductId = targetKitItem.ErpProductId;
                        orderItem.KitParentId = item.Id;
                        orderItem.PlacedName = targetKitItem.ItemName;
                        orderItem.Quantity = 1;
                        orderItem.KitItemIndex = kitItemIndex;

                        m_database.Save(orderItem);

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
                m_cache.Remove(c_cacheKey);
            }

            return GetKitForOrderItem(order, item);
        }
    }
}
