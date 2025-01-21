var app = app || {};
app.EshopMapping = app.EshopMapping || {};
app.EshopMapping.VM = app.EshopMapping.VM ||

    function () {
        const self = this;

        const defaultFilter = (x) => x;

        let currentFilter = defaultFilter;
        let currentExpandedItemId = null;
        let allEshopProducts = [];

        self.mappings = [];

        self.expandItem = function (itemId) {
            currentExpandedItemId = itemId;
            self.mappings.forEach(m => {
                m.expanded = m.itemId === currentExpandedItemId;
                m.addItemFormShown = false;
            });

            lt.notify();
        };

        self.unmapItem = function (materialName, productName) {
            lt.api("/eshopMapping/unmap")
                .query({ "elsaMaterialName": materialName, "eshopProductName": productName })
                .get(receiveMappings);
        };

        self.mapItem = function (materialName, productName, callback) {

            // TODO check already existing material mapping

            lt.api("/eshopMapping/map")
                .query({ "elsaMaterialName": materialName, "eshopProductName": productName })
                .get((received) => {
                    receiveMappings(received);
                    callback();
                });
        };

        self.getEshopProducts = function (query, callback) {
            callback(allEshopProducts);
        };

        self.getProductNameExists = function (name) {
            return allEshopProducts.indexOf(name) > -1;
        };

        const receiveMappings = (mappings) => {

            allEshopProducts = [];

            mappings.forEach(m => {
                m.itemId = m.ElsaMaterialName || m.Products.find(x => !!x.ProductName).ProductName;
                m.expanded = m.itemId === currentExpandedItemId;
                m.hasShopItem = false;
                m.firstShopItem = null;
                m.additionalShopItemsCount = 0;
                m.hasAdditionalShopItems = false;
                m.addItemFormShown = false;
                m.hasMaterial = !!m.ElsaMaterialName;

                m.Products.forEach(p => {

                    if (!allEshopProducts.includes(p.ProductName))
                        allEshopProducts.push(p.ProductName);

                    m.hasShopItem = true;

                    if (!m.firstShopItem) {
                        m.firstShopItem = p.ProductName;
                    } else {
                        m.additionalShopItemsCount++;
                        m.hasAdditionalShopItems = true;
                    }

                    p.connectedMaterial = m.ElsaMaterialName;
                    p.attributes = [];

                    if (p.OrderCount === 0) {
                        p.attributes.push({ text: "poslední dva roky neobjednáno" });
                    } else {
                        p.attributes.push({ text: p.OrderCount + " objednávek" });                        
                        p.attributes.push({ text: "Poslední objednávka " + p.LastOrderedAt })
                    }
                });
                
                m.additionalShopItemsText = null;
                if (m.additionalShopItemsCount > 0) {
                    m.additionalShopItemsText = "(+" + m.additionalShopItemsCount + ")";
                }

            });

            self.mappings = currentFilter(mappings);
        };

        let load = (reloadErp) => {
            lt.api("/eshopMapping/getMappings").query({ "reloadErpProducts": (!!reloadErp) }).get(receiveMappings);
        };

        load(false);

        
    };

app.EshopMapping.vm = app.EshopMapping.vm || new app.EshopMapping.VM();