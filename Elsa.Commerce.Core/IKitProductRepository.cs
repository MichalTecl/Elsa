﻿using System.Collections.Generic;

using Elsa.Commerce.Core.Model;
using Elsa.Core.Entities.Commerce.Commerce;
using Elsa.Core.Entities.Commerce.Inventory.Kits;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;

namespace Elsa.Commerce.Core
{
    public interface IKitProductRepository
    {
        IEnumerable<IKitDefinition> GetAllKitDefinitions();

        IKitDefinition GetKitDefinition(int id);

        IEnumerable<KitItemsCollection> GetKitForOrderItem(IPurchaseOrder order, IOrderItem item);

        IEnumerable<KitItemsCollection> SetKitItemSelection(IPurchaseOrder order, IOrderItem item, int kitItemId, int kitItemIndex);

        bool IsKit(IPurchaseOrder order, IOrderItem item);

        List<KitNoteParseResultModel> ParseKitNotes(long? orderId);

        ICollection<IKitDefinition> GetKitsByItemName(string itemName);

        void UpdateKitItemMapping(int kitItemId, string newItemName);
    }
}
