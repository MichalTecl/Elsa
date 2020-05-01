var app = app || {};
app.MaterialLevels = app.MaterialLevels || {};
app.MaterialLevels.VM = app.MaterialLevels.VM || function() {

    var self = this;

    var selectedInventoryId = 0;
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

            for (var j = 0; j < model.Batches.length; j++) {
                var b = model.Batches[j];
                b.batchLink = getBatchSearchLink(model.MaterialName, b.BatchNumber);
            }

            model.showSupplier = self.displaySupplier;
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
    
    self.onInventorySelected = function (inventoryId) {

        self.displaySupplier(null, null);

        if (self.inventories.length === 0) {
            setTimeout(function () { self.onInventorySelected(inventoryId); }, 100);
            return;
        }

        if (inventoryId === null || inventoryId === undefined) {
            for (let i = 0; i < self.inventories.length; i++) {
                if (self.inventories[i].hasWarning) {
                    self.onInventorySelected(self.inventories[i].Id);
                    return;
                }
            }

            for (let i = 0; i < self.inventories.length; i++) {
                if (self.inventories[i].Id > -1) {
                    self.onInventorySelected(self.inventories[i].Id);
                    return;
                }
            }

            self.onInventorySelected(-1);
            return;
        }
        
        if (selectedInventoryId === inventoryId) {
            return;
        }

        self.report = [];
        lt.notify();

        selectedInventoryId = inventoryId;
    
        for (let i = 0; i < self.inventories.length; i++) {
            self.inventories[i].isSelected = (self.inventories[i].Id === inventoryId);
            self.inventories[i].isSelectedNu = self.inventories[i].isSelected ? 1 : 0;
        }

        lt.notify();

        lt.api("/MaterialAmountReport/getLevels").query({ "inventoryId": inventoryId }).get(function(report) {
            receiveReport(report);
        });
    };

    var receiveInventory = function(inventoryModel) {

        var theModel = null;

        for (var i = 0; i < self.inventories.length; i++) {
            if (self.inventories[i].Id === inventoryModel.Id) {
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
        lt.api("/MaterialAmountReport/getUnwatchedInventories").get(function(unwa) {
            self.unwatchedInventories = unwa;
        });
    };

    setTimeout(loadInventories, 100);
    setTimeout(loadUnwatchedInventories, 500);

    var changeWatchedInventories = function(url, inventoryId) {
        self.report = [];
        self.inventories = [];
        self.unwatchedInventories = [];
        selectedInventoryId = 0;
        lt.notify();

        lt.api(url).query({"inventoryId":inventoryId}).get(function (inventories) {
            receiveInventories(inventories);
            self.onInventorySelected(inventoryId);
            loadUnwatchedInventories();
        });
    };

    self.watchInventory = function (inventoryId) {
        changeWatchedInventories("/MaterialAmountReport/watchInventory", inventoryId);
    };

    self.unwatchInventory = function (inventoryId) {
        changeWatchedInventories("/MaterialAmountReport/unwatchInventory", inventoryId);
    };

    self.refresh = function() {
        self.report = [];
        lt.notify();

        if (selectedInventoryId === 0) {
            self.onInventorySelected(null);
        }

        for (let i = 0; i < self.inventories.length; i++) {
            self.inventories[i].isSelected = (self.inventories[i].Id === selectedInventoryId);
            self.inventories[i].isSelectedNu = self.inventories[i].isSelected ? 1 : 0;
        }

        lt.notify();

        lt.api("/MaterialAmountReport/getLevels").query({ "inventoryId": selectedInventoryId }).get(function (report) {
            receiveReport(report);
        });
    };

    self.setThreshold = function(materialId, thresholdText) {
        lt.api("/MaterialAmountReport/setThreshold").query({ "materialId": materialId, "thresholdText": thresholdText })
            .post(function() {
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
};

app.MaterialLevels.vm = app.MaterialLevels.vm || new app.MaterialLevels.VM();