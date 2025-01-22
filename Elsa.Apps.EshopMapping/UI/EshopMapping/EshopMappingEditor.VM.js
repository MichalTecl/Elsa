var app = app || {};
app.EshopMapping = app.EshopMapping || {};
app.EshopMapping.VM = app.EshopMapping.VM ||

    function () {
        const self = this;

        let allMappings = [];
                
        let currentExpandedItemId = null;
        let allEshopProducts = [];
        let allSearchEntries = [];
        let textFilter = null;
        let showOnlyIncompletes = false;

        self.mappings = [];

        var showMappings = () => {

            var matcher = new TextMatcher(textFilter);

            self.mappings = allMappings.filter(m => {

                if (showOnlyIncompletes && m.hasShopItem && m.hasMaterial) {
                    return false;
                }

                if ((!!textFilter) && (!matcher.match(m.searchTag, true))) {
                    return false;
                }

                return true;
            });

            lt.notify();
        };

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

        self.getAllSearchEntries = function (query, callback) {
            callback(allSearchEntries);
        };

        self.filter = function (textQuery, onlyIncomplete) {
            textFilter = textQuery;
            showOnlyIncompletes = onlyIncomplete;

            showMappings();
        };

        self.refreshErp = function () {
            self.mappings = [];
            lt.notify();

            load(true);
        };

        const receiveMappings = (mappings) => {

            allEshopProducts = [];
            allSearchEntries = [];

            mappings.forEach(m => {
                m.itemId = m.ElsaMaterialName || m.Products.find(x => !!x.ProductName).ProductName;
                m.expanded = m.itemId === currentExpandedItemId;
                m.hasShopItem = false;
                m.firstShopItem = null;
                m.additionalShopItemsCount = 0;
                m.hasAdditionalShopItems = false;
                m.addItemFormShown = false;
                m.hasMaterial = !!m.ElsaMaterialName;

                m.searchTagItems = [];
                m.searchTagItems.push(m.ElsaMaterialName);


                if (!allSearchEntries.includes(m.ElsaMaterialName))
                    allSearchEntries.push(m.ElsaMaterialName);

                m.Products.forEach(p => {

                    if (!allEshopProducts.includes(p.ProductName))
                        allEshopProducts.push(p.ProductName);

                    if (!allSearchEntries.includes(m.ElsaMaterialName))
                        allSearchEntries.push(p.ProductName);

                    m.searchTagItems.push(p.ProductName);

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

                m.searchTag = m.searchTagItems.join("|");

                m.additionalShopItemsText = null;
                if (m.additionalShopItemsCount > 0) {
                    m.additionalShopItemsText = "(+" + m.additionalShopItemsCount + ")";
                }

            });

            allMappings = mappings;
            showMappings();
        };

        let load = (reloadErp) => {
            lt.api("/eshopMapping/getMappings").query({ "reloadErpProducts": (!!reloadErp) }).get(receiveMappings);
        };

        load(false);        
    };

app.EshopMapping.vm = app.EshopMapping.vm || new app.EshopMapping.VM();