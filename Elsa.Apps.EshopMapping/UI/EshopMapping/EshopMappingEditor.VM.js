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
        let showOnlyMultimaps = false;

        let currentPickedElsaMaterial = null;
        let currentPickedEshopProduct = null;

        self.mappings = [];
        self.isPuzzleActive = false;
        self.currentPuzzleValue = null;

        var updatePuzzleState = () => {

            self.isPuzzleActive = !!currentPickedElsaMaterial || !!currentPickedEshopProduct;
            self.currentPuzzleValue = currentPickedElsaMaterial || currentPickedEshopProduct;

            const pst = (m) => {

                // Initially the puzzle is shown only for items where one side is missing
                if (!currentPickedElsaMaterial && !currentPickedEshopProduct && m.hasMaterial && m.hasShopItem)
                    return false;
                                    
                if (m.hasMaterial && !!currentPickedElsaMaterial)
                    return false;

                if (m.hasShopItem && !!currentPickedEshopProduct)
                    return false;

                return true;
            };
            
            self.mappings.forEach(m => {
                m.showPuzzle = pst(m);
            });
        };

        var showMappings = () => {

            var matcher = new TextMatcher(textFilter);

            self.mappings = allMappings.filter(m => {

                if (showOnlyIncompletes && m.hasShopItem && m.hasMaterial) {
                    return false;
                }

                if (showOnlyMultimaps && !m.hasAdditionalShopItems) {
                    return false;
                }
                                
                if ((!!textFilter) && (!matcher.match(m.searchTag, true))) {
                    return false;
                }

                return true;
            });

            updatePuzzleState();
            
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

        self.hideMaterial = function (name) {
            lt.api("/eshopMapping/hideMaterial").query({ "elsaMaterialName": name }).get(receiveMappings);
        };

        self.getAllSearchEntries = function (query, callback) {
            callback(allSearchEntries);
        };

        self.filter = function (textQuery, onlyIncomplete, onlyMultimaps) {
            textFilter = textQuery;
            showOnlyIncompletes = onlyIncomplete;
            showOnlyMultimaps = onlyMultimaps;

            showMappings();
        };

        self.refreshErp = function () {
            self.mappings = [];
            lt.notify();

            load(true);
        };

        self.onPuzzleActivated = function (record) {

            currentPickedElsaMaterial = currentPickedElsaMaterial || record.ElsaMaterialName;
            currentPickedEshopProduct = currentPickedEshopProduct || record.firstShopItem;
            
            if (!!currentPickedElsaMaterial && !!currentPickedEshopProduct) {

                self.mapItem(currentPickedElsaMaterial, currentPickedEshopProduct, () => {
                    currentPickedEshopProduct = null;
                    currentPickedElsaMaterial = null;
                    updatePuzzleState();
                })

                return;
            }

            updatePuzzleState();
            lt.notify();
        };

        self.cancelPuzzle = () => {
            currentPickedElsaMaterial = null;
            currentPickedEshopProduct = null;

            updatePuzzleState();
            lt.notify();
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