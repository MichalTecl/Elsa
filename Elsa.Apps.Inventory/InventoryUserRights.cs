﻿using Elsa.Common.Interfaces;
using Elsa.Users.Infrastructure;

namespace Elsa.Apps.Inventory
{
    [UserRights]
    public static class InventoryUserRights
    {
        public static readonly UserRight ViewWarehouseWidget = new UserRight(nameof(ViewWarehouseWidget), "Sklady");
        public static readonly UserRight MaterialBatchesViewer = new UserRight(nameof(MaterialBatchesViewer), "Procházení šarží", ViewWarehouseWidget);
        public static readonly UserRight MaterialBatchesDeleting = new UserRight(nameof(MaterialBatchesDeleting), "Úpravy a mazání šarží", MaterialBatchesViewer);
        public static readonly UserRight StockEventsView = new UserRight(nameof(StockEventsView), "Karta Odpisy", MaterialBatchesViewer);
        public static readonly UserRight StockEventsCreation = new UserRight(nameof(StockEventsCreation), "Smí vytvářet Odpad/Propagace", StockEventsView);
        public static readonly UserRight MaterialStockInApp = new UserRight(nameof(MaterialStockInApp), "Karta Naskladnění", MaterialBatchesViewer);
        public static readonly UserRight ProductionApp = new UserRight(nameof(ProductionApp), "Karta Výroba", MaterialBatchesViewer);
        public static readonly UserRight DirectSalesApp = new UserRight(nameof(DirectSalesApp), "Karta Prodejní akce", MaterialBatchesViewer);
        public static readonly UserRight WhConfiguration = new UserRight(nameof(WhConfiguration), "Sklady - Konfigurace", ViewWarehouseWidget);
        public static readonly UserRight ProductsAndTags = new UserRight(nameof(ProductsAndTags), "Produkty & Tagy", WhConfiguration);
        public static readonly UserRight Materials = new UserRight(nameof(Materials), "Materiály", WhConfiguration);
        public static readonly UserRight MaterialEdits = new UserRight(nameof(MaterialEdits), "Úpravy materiálu", Materials);
        public static readonly UserRight MaterialLevels = new UserRight(nameof(MaterialLevels), "Přehled zásob", WhConfiguration);
        public static readonly UserRight ViewSuppliers = new UserRight(nameof(ViewSuppliers), "Přehled zásob - zobrazit dodavatele", MaterialLevels);
    }
}
