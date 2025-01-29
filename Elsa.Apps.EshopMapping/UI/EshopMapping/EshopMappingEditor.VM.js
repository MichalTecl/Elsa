var app = app || {};
app.EshopMapping = app.EshopMapping || {};
app.EshopMapping.VM = app.EshopMapping.VM ||

    function () {
        const self = this;

        let allMappings = [];
                
        let currentExpandedItemId = null;
        let allEshopProducts = [];
        let unboundEshopProducts = [];
        let allSearchEntries = [];

        let textMatcher = null;

        const filters = {
            "text": { active: true, predicate: (m) => (textMatcher == null) || textMatcher.match(m.searchTag, true) },
            "incomplete": { active: false, predicate: (m) => !(m.hasShopItem && (m.hasMaterial || m.isKit)) },
            "multimaps": { active: false, predicate: (m) => m.hasAdditionalShopItems },
            "kits": { active: false, predicate: (m) => m.isKit }
        };
       
        let currentPickedElsaMaterial = null;
        let currentPickedEshopProduct = null;

        self.mappings = [];
        self.isPuzzleActive = false;
        self.currentPuzzleValue = null;

        var updatePuzzleState = () => {

            self.isPuzzleActive = !!currentPickedElsaMaterial || !!currentPickedEshopProduct;
            self.currentPuzzleValue = currentPickedElsaMaterial || currentPickedEshopProduct;

            const pst = (m) => {

                if (m.isKit)
                    return false;

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

            var afilters = Object.values(filters).filter(f => f.active).map(f => f.predicate);

            self.mappings = allMappings;

            self.mappings.forEach(m => {
                m.isVisible = true;

                if (m.expanded) {
                    return;
                }

                var discriminant = afilters.find(f => !f(m));
                if (!!discriminant) {
                    m.isVisible = false;
                }

            });
                        
            updatePuzzleState();
            
            lt.notify();
        };

        self.expandItem = function (itemId) {
            currentExpandedItemId = itemId;
            self.mappings.forEach(m => {
                m.expanded = m.itemId === currentExpandedItemId;

                if (!m.expanded)
                    m.pinned = false;

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

        self.getUnboundEshopProducts = function (query, callback) {
            callback(unboundEshopProducts);
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

        /*
        self.filter = function (textQuery, onlyIncomplete, onlyMultimaps) {
            textFilter = textQuery;
            showOnlyIncompletes = onlyIncomplete;
            showOnlyMultimaps = onlyMultimaps;

            self.expandItem(null);

            showMappings();
        };
        */

        self.setTextFilter = (pattern) => {
            textMatcher = new TextMatcher(pattern);

            showMappings();
        };

        self.setFilter = (name, active) => {
            filters[name].active = active;
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
            unboundEshopProducts = [];

            let productMap = {};
            let kitDefinitions = [];
            let defaultErpIconUrl = null;
            
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
                m.isKit = false;

                m.requiresMapping = true;
                
                if (!allSearchEntries.includes(m.ElsaMaterialName))
                    allSearchEntries.push(m.ElsaMaterialName);

                m.Products.forEach(p => {

                    defaultErpIconUrl = defaultErpIconUrl || p.ErpIconUrl;

                    productMap[p.ProductName] = p;

                    if (!m.singleProduct)
                        m.singleProduct = p;

                    p.canUnlinkFromMaterial = m.hasMaterial;

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

                    const statsSrc = p.OrderingInfo || {};
                    const stats = {
                        "orderCountNoKit": statsSrc.OrderCountNoKit || 0,
                        "orderCountInKit": statsSrc.OrderCountInKit || 0,
                        "lastOrderDtNoKit": statsSrc.LastOrderDtNoKit || '',
                        "lastOrderDtKit": statsSrc.LastOrderDtKit || ''
                    };
                    stats.totalOrders = stats.orderCountInKit + stats.orderCountNoKit;

                    const attr = (text, detailUrl) => {
                        p.attributes.push({ "text": text, "hasDetail": !!detailUrl, "detailUrl": detailUrl });
                    };

                    if (!!p.KitDefinition) {
                        attr("SADA");
                        kitDefinitions.push(p.KitDefinition);
                    }

                    if (!p.ErpProductExists)
                        attr("Produkt neexistuje ve Floxu");

                    if (stats.totalOrders === 0) {
                        attr("poslední dva roky neobjednáno");
                    } else {

                        if (stats.orderCountInKit === 0) {
                            attr(stats.orderCountNoKit + " objednávek", '/eshopMapping/peekOrders?kit=false&placedName=' + encodeURIComponent(p.ProductName));
                            attr("Poslední objednávka " + stats.lastOrderDtNoKit);
                        } else {
                                                                                                             
                            attr(stats.orderCountInKit + " objednávek v sadě", '/eshopMapping/peekOrders?kit=true&placedName=' + encodeURIComponent(p.ProductName));
                            attr("Poslední objednávka v sadě " + stats.lastOrderDtKit);

                            if (stats.orderCountNoKit > 0) {
                                attr(stats.orderCountNoKit + " objednávek mimo sady", '/eshopMapping/peekOrders?kit=false&placedName=' + encodeURIComponent(p.ProductName));
                                attr("Poslední objednávka mimo sady " + stats.lastOrderDtNoKit);
                            } else {
                                attr("Žádné objednávky mimo sady");
                            }
                        }
                    }

                    p.OwningKits.forEach(owk => {
                        p.attributes.push({ text: "Patří do sady \"" + owk + "\"", hasDetail: false });
                    });

                    if (!!p.KitDefinition) {
                        p.isKit = true;
                        m.isKit = true;
                        m.requiresMapping = false;
                    } else {
                        p.isKit = false;
                    }

                    if (!m.isKit && !m.hasMaterial) {
                        unboundEshopProducts.push(p.ProductName);
                    }
                });

                m.searchTag = m.searchTagItems.join("|");

                m.additionalShopItemsText = null;
                if (m.additionalShopItemsCount > 0) {
                    m.additionalShopItemsText = "(+" + m.additionalShopItemsCount + ")";
                }

                m.showMissingMaterial = !(m.isKit || m.hasMaterial);
                                
                m.sorterGroup = 0;

                m.canAddProduct = true;

                if (m.isKit) {
                    m.sorterGroup = 10;
                    m.canAddProduct = false;
                }
                else if (!m.hasShopItem) {
                    m.sorterGroup = 20;
                } else if (!m.hasMaterial) {
                    m.sorterGroup = 30;
                    m.canAddProduct = false;
                }
            });

            kitDefinitions.forEach(kd =>
                kd.SelectionGroups.forEach(sg =>
                    sg.Items.forEach(
                        item => {
                            var sourceProduct = productMap[item.ItemName] || {};
                            item.erpProductExists = !!sourceProduct.ErpProductExists;
                            item.erpIconUrl = sourceProduct.ErpIconUrl || allEshopProducts[0].ErpIconUrl;
                            item.isEditing = false;
                        }
                    )));

            allMappings = mappings.sort((a, b) => {
                // First sort by sorterGroup
                if (a.sorterGroup !== b.sorterGroup) {
                    return a.sorterGroup - b.sorterGroup;
                }
                // If sorterGroup is the same, sort by searchTag
                return a.searchTag.localeCompare(b.searchTag);
            });
           
            showMappings();
        };

        self.showAttributeDetail = (detailUrl) => {
            lt.api(detailUrl)                
                .get((str) => {                    
                    alert(str);
                });
        };

        self.updateKitItem = (kitItemId, newItemName) => {
            lt.api("/eshopMapping/updateKitItem")
                .query({ kitItemId, newItemName })
                .post(receiveMappings);
        };

        let load = (reloadErp) => {
            lt.api("/eshopMapping/getMappings").query({ "reloadErpProducts": (!!reloadErp) }).get(receiveMappings);
        };

        load(false);        
    };

app.EshopMapping.vm = app.EshopMapping.vm || new app.EshopMapping.VM();