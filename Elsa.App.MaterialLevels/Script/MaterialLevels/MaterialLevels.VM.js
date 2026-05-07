var app = app || {};
app.MaterialLevels = app.MaterialLevels || {};
app.MaterialLevels.VM = app.MaterialLevels.VM || function() {

    var self = this;

    var selectedTabKey = null;
    var filter = null;

    self.isDisabled = false;
    self.isLoading = true;
    self.hasWarnings = false;
    self.warnings = "";
    self.inventories = [];
    self.report = [];
    self.unwatchedInventories = [];
    self.showSupplier = false;
    self.supplierMail = "";
    self.supplierMailto = "";
    self.supplierPhone = "";

    var applyFilter = function() {
        var matcher = new TextMatcher(filter);
        var changed = false;
        for (var i = 0; i < self.report.length; i++) {
            var hide = !matcher.match(self.report[i].MaterialName + self.report[i].SupplierName, true);
            if (self.report[i].hidden !== hide) {
                changed = true;
                self.report[i].hidden = hide;
            }
        }

        if (changed) {
            lt.notify();
        }
    };

    var getBatchSearchLink = function(materialName, batchNumber) {
        return "/UI/Pages/Inventory/WhEvents.html?#findBatches=" +
            encodeURIComponent(JSON.stringify({ "materialName": materialName, "batchNumber": batchNumber }));
    };

    var getTabQuery = function(tab) {
        var query = { "inventoryId": tab.InventoryId };
        if (tab.MaterialLevelReportingGroup) {
            query.materialLevelReportingGroup = tab.MaterialLevelReportingGroup;
        }

        return query;
    };

    var receiveReport = function (data) {

        self.showSupplier = false;

        for (let row of data) {
            if (row.SupplierName) {
                self.showSupplier = true;
                break;
            }
        }

        for (var i = 0; i < data.length; i++) {
            var model = data[i];

            model.materialLink = getBatchSearchLink(model.MaterialName, null);
            model.hasThreshold = model.ThresholdFormatted !== null;
            model.displaySupplier = self.showSupplier;
            model.hasOrderDt = !!model.OrderDt;
            model.hasComment = !!model.CommentText;
            model.hasDeadline = !!model.DeliveryDeadline;

            model.data = { MaterialId: model.MaterialId };            
            model.callback = () => {
                self.refresh();
                Popup.close();
            };

            for (var j = 0; j < model.Batches.length; j++) {
                var b = model.Batches[j];
                b.batchLink = getBatchSearchLink(model.MaterialName, b.BatchNumber);
            }

            model.showSupplier = self.displaySupplier;
            model.enteringOrderDt = false;

            model.warnLevelClass = "lt-template gridRow warnLevelClass" + model.WarningLevel;
        }

        self.report = data;
        applyFilter();
    };

    self.displaySupplier = function (mail, phone) {
        if (mail) {
            self.supplierMailto = "mailto:" + mail;
        } else {
            self.supplierMailto = "";
        }

        self.supplierMail = mail || "";
        self.supplierPhone = phone || "";

        lt.notify();
    };
    
    var findInventoryTab = function(key) {
        for (var i = 0; i < self.inventories.length; i++) {
            if (self.inventories[i].Key === key) {
                return self.inventories[i];
            }
        }

        return null;
    };

    self.onInventorySelected = function (tabKey) {

        self.displaySupplier(null, null);

        if (self.inventories.length === 0) {
            setTimeout(function () { self.onInventorySelected(tabKey); }, 100);
            return;
        }

        if (tabKey === null || tabKey === undefined) {
            for (let i = 0; i < self.inventories.length; i++) {
                if (self.inventories[i].hasWarning) {
                    self.onInventorySelected(self.inventories[i].Key);
                    return;
                }
            }

            for (let i = 0; i < self.inventories.length; i++) {
                if (self.inventories[i].Id > -1) {
                    self.onInventorySelected(self.inventories[i].Key);
                    return;
                }
            }

            self.onInventorySelected("-1|");
            return;
        }
        
        if (selectedTabKey === tabKey) {
            return;
        }

        var selectedTab = findInventoryTab(tabKey);
        if (!selectedTab) {
            self.onInventorySelected(null);
            return;
        }

        self.report = [];
        lt.notify();

        selectedTabKey = tabKey;
    
        for (let i = 0; i < self.inventories.length; i++) {
            self.inventories[i].isSelected = (self.inventories[i].Key === tabKey);
            self.inventories[i].isSelectedNu = self.inventories[i].isSelected ? 1 : 0;
        }

        lt.notify();

        lt.api("/MaterialAmountReport/getLevels")
            .query(getTabQuery(selectedTab))
            .get(function(report) {
            receiveReport(report);
        });
    };

    var receiveInventory = function(inventoryModel) {

        var theModel = null;

        for (var i = 0; i < self.inventories.length; i++) {
            if (self.inventories[i].Key === inventoryModel.Key) {
                theModel = self.inventories[i];
                break;
            }
        }

        if (!theModel) {
            theModel = inventoryModel;
            if (inventoryModel.Id !== -1) {
                self.inventories.push(theModel);
            }
        }
        
        theModel.isLoading = inventoryModel.WarningsCount === null;
        theModel.WarningsCount = inventoryModel.WarningsCount;
        theModel.hasWarning = (inventoryModel.WarningsCount || 0) > 0;
        theModel.Warnings = inventoryModel.Warnings;
        theModel.isSelected = theModel.isSelected || false;
        theModel.isSelectedNu = theModel.isSelected ? 1 : 0;
        theModel.isAggregation = theModel.Id < 1;
        
        if (theModel.Id === -1 && theModel.WarningsCount !== null && theModel.WarningsCount !== undefined) {
            self.isLoading = false;
            self.hasWarnings = theModel.WarningsCount > 0;
            self.warnings = theModel.Warnings;
        }
    };

    var receiveInventories = function(inventories) {

        self.isDisabled = false;
        if (inventories.length === 0) {
            self.isLoading = false;
            self.hasWarnings = false;
            self.isDisabled = true;
            return;
        }

        var needsFull = false;
        for (var i = 0; i < inventories.length; i++) {
            receiveInventory(inventories[i]);

            if (inventories[i].WarningsCount === null) {
                needsFull = true;
            }
        }

        if (needsFull) {
            loadInventories(true);
        } else {
            self.isLoading = false;
        }
    };

    var loadInventories = function (full) {

        if (full) {
            self.isLoading = true;
            lt.notify();
        }

        lt.api("/MaterialAmountReport/getInventories").query({"quick": !full}).silent().get(receiveInventories);
    };

    var loadUnwatchedInventories = function() {
        lt.api("/MaterialAmountReport/getUnwatchedInventories").silent().get(function(unwa) {
            self.unwatchedInventories = unwa;
        });
    };

    setTimeout(loadInventories, 100);
    setTimeout(loadUnwatchedInventories, 500);

    var changeWatchedInventories = function(url, tab, selectAfterChange) {
        self.report = [];
        self.inventories = [];
        self.unwatchedInventories = [];
        selectedTabKey = null;
        lt.notify();

        lt.api(url)
            .query(getTabQuery(tab))
            .get(function (inventories) {
            receiveInventories(inventories);
            self.onInventorySelected(selectAfterChange ? tab.Key : null);
            loadUnwatchedInventories();
        });
    };

    self.watchInventory = function (tab) {
        changeWatchedInventories("/MaterialAmountReport/watchInventory", tab, true);
    };

    self.unwatchInventory = function (tab) {
        changeWatchedInventories("/MaterialAmountReport/unwatchInventory", tab, false);
    };

    self.refresh = function() {
        self.report = [];
        lt.notify();

        if (!selectedTabKey) {
            self.onInventorySelected(null);
        }

        var selectedTab = findInventoryTab(selectedTabKey);
        if (!selectedTab) {
            self.onInventorySelected(null);
            return;
        }

        for (let i = 0; i < self.inventories.length; i++) {
            self.inventories[i].isSelected = (self.inventories[i].Key === selectedTabKey);
            self.inventories[i].isSelectedNu = self.inventories[i].isSelected ? 1 : 0;
        }

        lt.notify();

        lt.api("/MaterialAmountReport/getLevels")
            .query(getTabQuery(selectedTab))
            .get(function (report) {
                receiveReport(report);
            });
    };

    self.setThreshold = function(materialId, thresholdText) {
        lt.api("/MaterialAmountReport/setThreshold").query({ "materialId": materialId, "thresholdText": thresholdText })
            .post(function() {
                self.refresh();
            });
    };

    self.setOrderDtEditing = function (materialId) {
        for (var i in self.report) {
            self.report[i].enteringOrderDt = (self.report[i].MaterialId === materialId);
        }

        lt.notify();
    };

    self.setOrderDt = function (materialId, value) {
        lt.api("/MaterialAmountReport/setOrderDt")
            .query({ "materialId": materialId, "value": value })
            .post(function (actualized) {
                self.refresh();
            })
    };

    self.setMaterialComment = function (materialId, newComment) {
        lt.api("/MaterialAmountReport/setComment")
            .query({ "materialId": materialId, "text": newComment })
            .post(function () {
                self.refresh();
            });
    };

    self.setFilter = function(value) {
        if (filter === value) {
            return;
        }
        
        filter = value;
        applyFilter();

        self.displaySupplier(null, null);
    };

    self.deleteDeadline = function (materialId) {

        if (!confirm("Smazat ručně nastavený limit dodání?"))
            return;

        lt.api("/MaterialAmountReport/deleteDeadline")
            .query({ "materialId": materialId })
            .post(function () {
                self.refresh();
            });
    };
};

app.MaterialLevels.vm = app.MaterialLevels.vm || new app.MaterialLevels.VM();
