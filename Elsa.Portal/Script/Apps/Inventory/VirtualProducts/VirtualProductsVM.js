var app = app || {};
app.virtualProductsEditor = app.virtualProductsEditor || {};
app.virtualProductsEditor.ViewModel = app.virtualProductsEditor.ViewModel || function() {

    var self = this;

    self.selectedMappables = [];
    self.currentQuery = "";

    self.selectedVirtualProducts = [];
    self.selectedMaterials = [];

    self.currentMaterialInventory = null;

    var materialInventories = null;
    var matInventoriesCallbacks = [];

    var setupAbandonedBatchesStuff = function (mat) {

        var unuDays = parseInt(mat.DaysBeforeWarnForUnused);
        if (isNaN(unuDays)) {
            mat.detectAbandoned = false;
            mat.DaysBeforeWarnForUnused = null;
            mat.abandonTriggerIsManufacturing = false;
            mat.abandonTriggerIsUsage = false;
            mat.abdActionIsReport = false;
            mat.abdActionIsAutofinalize = false;
        } else {
            mat.DaysBeforeWarnForUnused = unuDays;
            mat.detectAbandoned = true;
            mat.abandonTriggerIsUsage = !!mat.UsageProlongsLifetime;
            mat.abandonTriggerIsManufacturing = !mat.abandonTriggerIsUsage;

            mat.abdActionIsAutofinalize = !!mat.Autofinalization;
            mat.abdActionIsReport = (!!mat.UnusedWarnMaterialType) && (!mat.abdActionIsAutofinalize);
        }

        mat.rbGroupAction = "radio_action" + (mat.Id || new Date().getTime());
        mat.rbGroupTrigger = "radio_trigger" + (mat.Id || new Date().getTime());
    };

    var adjustServerMaterial = function(mat) {

        mat.editMode = false;

        var materials = [];
        
        mat.materials = materials;
        
        mat.nominalAmountText = mat.NominalAmount + mat.NominalUnit.Symbol;

        setupAbandonedBatchesStuff(mat);
        
    };

    var receiveMaterials = function (mats) {

        for (var i = 0; i < mats.length; i++) {
            adjustServerMaterial(mats[i]);
        }

        self.selectedMaterials = mats;
        
    };

    var receiveSingleMaterial = function(mat) {

        adjustServerMaterial(mat);

        try {

            for (var i = 0; i < self.selectedMaterials.length; i++) {
                if (self.selectedMaterials[i].Id === mat.Id) {
                    self.selectedMaterials[i] = mat;
                    return;
                }
            }

            self.selectedMaterials.push(mat);

        } finally {
            self.cancelMaterialEdit(true);
        }
    };

    var receiveMappables = function (mappables) {

        for (var i = 0; i < mappables.length; i++) {
            var mappable = mappables[i];

            for (var j = 0; j < mappable.AssignedVirtualProducts.length; j++) {
                mappable.AssignedVirtualProducts[j].ownerMappableId = JSON.parse(mappable.Id);
            }
        }

        self.selectedMappables = mappables;
    };

    var adjustServerVpObject = function(vp) {
        var unhashedName = vp.Name;
        if ((!!unhashedName) && (unhashedName.indexOf("#") === 0)) {
            unhashedName = unhashedName.substr(1);
        }

        vp.materials = vp.MaterialEntries;
        vp.unhashedName = unhashedName;
        vp.editMode = false;
    };

    var receiveVirtualProducts = function (vps) {

        self.selectedVirtualProducts = vps;

        for (var i = 0; i < vps.length; i++) {
            var vp = vps[i];

            adjustServerVpObject(vp);
        }

        self.cancelVpEdit(true);
    };

    var receiveSingleVp = function(vp) {
        adjustServerVpObject(vp);

        var found = false;

        for (var i = 0; i < self.selectedVirtualProducts.length; i++) {
            if (self.selectedVirtualProducts[i].VirtualProductId === vp.VirtualProductId) {
                self.selectedVirtualProducts[i] = vp;
                found = true;
                break;
            }
        }

        if (!found) {
            self.selectedVirtualProducts.push(vp);
        }
        
        lt.notify();
    };

    self.loadMappables = function (searchQuery) {
        self.currentQuery = searchQuery;
        lt.api("/virtualProducts/getMappableItems").query({ "searchQuery": searchQuery }).get(receiveMappables);
    };

    self.unmap = function(mappable) {

        lt.api("/virtualProducts/removeVirtualProductMapping")
            .query({ "virtualProductId": mappable.VirtualProductId, "activeSearchQuery": self.currentQuery })
            .body(mappable.ownerMappableId)
            .post(receiveMappables);
    };

    self.searchVirtualProducts = function(query, callback) {
        lt.api("/virtualProducts/getVirtualProducts").query({ "searchQuery": query }).get(callback);
    };

    self.mapVpToItem = function(mappable, virtualProduct) {
        lt.api("/virtualProducts/mapOrderItemToProduct")
        .query({ "virtualProductId": virtualProduct.VirtualProductId, "activeSearchQuery": self.currentQuery })
            .body(JSON.parse(mappable.Id))
            .post(receiveMappables);
    };

    self.loadVirtualProducts = function(query) {
        lt.api("/virtualProducts/getVirtualProducts")
            .query({ searchQuery: query })
            .get(receiveVirtualProducts);
    };
    
    self.cancelVpEdit = function(doNotNotify) {

        if (self.selectedVirtualProducts.length > 0 && (!self.selectedVirtualProducts[0].VirtualProductId)) {
            self.selectedVirtualProducts.splice(0, 1);
        }

        for (var i = 0; i < self.selectedVirtualProducts.length; i++) {

            if (!!self.selectedVirtualProducts[i].editMode) {
                lt.api("/virtualProducts/getVirtualProductById")
                    .query({ "id": self.selectedVirtualProducts[i].VirtualProductId })
                    .get(receiveSingleVp);
            }

            self.selectedVirtualProducts[i].editMode = false;
        }

        if (!doNotNotify) {
            lt.notify();
        }
    };

    self.setVirtualProductEdit = function(vpId) {

        self.cancelVpEdit(true);

        var found = false;

        for (var i = 0; i < self.selectedVirtualProducts.length; i++) {

            var vp = self.selectedVirtualProducts[i];
            if (vp.VirtualProductId === vpId) {
                vp.editMode = true;
                found = true;
            } else {
                vp.editMode = false;
            }
        }

        if (!found) {
            var newVp = { editMode: true, materials: [] };
            self.selectedVirtualProducts.unshift(newVp);
        }

        lt.notify();
    };

    self.saveVirtualProduct = function(model) {

        lt.api("/virtualProducts/saveVirtualProduct").body(model).post(function(savedVp) {

            self.cancelVpEdit(true);

            receiveSingleVp(savedVp);

        });

    };

    self.deleteVirtualProduct = function(vpId) {
        lt.api("/virtualProducts/deleteVirtualProduct").query({ "vpId": vpId }).get(function(result) {
            
            for (var i = 0; i < self.selectedVirtualProducts.length; i++) {
                if (self.selectedVirtualProducts[i].VirtualProductId === vpId) {
                    self.selectedVirtualProducts.splice(i, 1);
                    lt.notify();
                    return;
                }
            }

        });
    };

    self.searchMaterials = function (query) {

        var inventoryId = null;
        if (self.currentMaterialInventory) {
            inventoryId = self.currentMaterialInventory.Id;
        }

        lt.api("/virtualProducts/searchMaterials").body(query).query({ "inventoryId":inventoryId }).post(receiveMaterials);
    };

    self.cancelMaterialEdit = function(doNotNotify) {
        
        if (self.selectedMaterials.length > 0 && (!self.selectedMaterials[0].Id)) {
            self.selectedMaterials.splice(0, 1);
        }

        for (var i = 0; i < self.selectedMaterials.length; i++) {

            if (!!self.selectedMaterials[i].editMode) {
                lt.api("/virtualProducts/getMaterialById")
                    .query({ "id": self.selectedMaterials[i].Id })
                    .get(receiveSingleMaterial);
            }

            self.selectedMaterials[i].editMode = false;
        }

        if (!doNotNotify) {
            lt.notify();
        }
    };

    self.setMaterialEdit = function(id) {

        self.cancelMaterialEdit(true);

        var found = false;
        for (var i = 0; i < self.selectedMaterials.length; i++) {
            if (self.selectedMaterials[i].Id === id) {
                self.selectedMaterials[i].editMode = true;
                found = true;
                break;
            }
        }

        if (!found) {

            var newMat = { editMode: true, materials: [], RequiresPrice: self.currentMaterialInventory.RequirePriceDefault || false, RequiresInvoice: self.currentMaterialInventory.RequireInvoicesDefault };
            if (self.currentMaterialInventory.AllowedUnit) {
                newMat.nominalAmountText = "1" + self.currentMaterialInventory.AllowedUnit.Symbol;
            }

            setupAbandonedBatchesStuff(newMat);

            self.selectedMaterials.unshift(newMat);
        }

        lt.notify();
    };

    self.saveMaterial = function (model) {

        if (!model.detectAbandoned) {
            model.DaysBeforeWarnForUnused = null;
        } else {

            model.DaysBeforeWarnForUnused = parseInt(model.DaysBeforeWarnForUnused);
            if (isNaN(model.DaysBeforeWarnForUnused)) {
                alert("Pro zpracování opuštěných šarží musí být vyplněn počet dnů");
                return;
            }

            if ((!model.abandonTriggerIsManufacturing) && (!model.abandonTriggerIsUsage)) {
                alert("Musí být zvolen způsob výpočtu doby opuštění šarže (od výroby / od polsedního použití)");
                return;
            }

            if ((!model.abdActionIsAutofinalize) && (!model.abdActionIsReport)) {
                alert("Musí být zvolena akce pro opuštěné šarže");
                return;
            }

            if ((!!model.abdActionIsReport) && (!model.UnusedWarnMaterialType)) {
                alert("Pro varování o opuštěných šarží musí být vyplněna skupina upozornění");
                return;
            }

            model.UsageProlongsLifetime = !!model.abandonTriggerIsUsage;
        }
        
        var request = {
            MaterialId: model.Id,
            MaterialName: model.Name,
            NominalAmountText: model.nominalAmountText,
            MaterialInventoryId: self.currentMaterialInventory.Id,
            AutomaticBatches: model.AutomaticBatches,
            RequiresPrice: model.RequiresPrice,
            RequiresProductionPrice: model.RequiresProductionPrice,
            RequiresInvoice: model.RequiresInvoice,
            RequiresSupplierReference:model.RequiresSupplierReference,
            Materials: [],
            HasThreshold: model.HasThreshold,
            ThresholdText: model.ThresholdText,
            Autofinalization: model.Autofinalize || (!!model.abdActionIsAutofinalize),
            CanBeDigital: model.CanBeDigital,
            DaysBeforeWarnForUnused: model.detectAbandoned ? model.DaysBeforeWarnForUnused : null,
            UnusedWarnMaterialType: model.UnusedWarnMaterialType,
            UsageProlongsLifetime: model.UsageProlongsLifetime
        };

        for (var i = 0; i < model.materials.length; i++) {
            request.Materials.push({ DisplayText: model.materials[i].displayText });
        }

        lt.api("/virtualProducts/saveMaterial").body(request).post(receiveSingleMaterial);
        
    };

    self.deleteMaterial = function(id) {

        lt.api("/virtualProducts/deleteMaterial").query({ id: id }).get(function() {
            for (var i = 0; i < self.selectedMaterials.length; i++) {
                if (self.selectedMaterials[i].Id === id) {
                    self.selectedMaterials.splice(i, 1);
                    return;
                }
            }
        });

    };

    self.getMaterialInventories = function(callback) {
        if (!!materialInventories) {
            callback(materialInventories);
            return;
        }

        matInventoriesCallbacks.push(callback);
    };

    self.loadMaterialInventories = function() {
        lt.api("/virtualProducts/getMaterialInventories").get(function(inventories) {
            materialInventories = inventories;

            while (matInventoriesCallbacks.length > 0) {
                var callback = matInventoriesCallbacks.shift();
                callback(materialInventories);
            }
        });
    };

    setTimeout(self.searchMaterials, 500);
    setTimeout(self.loadVirtualProducts, 100);
    setTimeout(self.loadMaterialInventories, 200);

    self.setCurrentMaterialInventory = function(inventoryId) {
        if (self.currentMaterialInventory && self.currentMaterialInventory.Id === inventoryId) {
            return;
        }

        self.selectedMaterials = [];
        lt.notify();

        self.getMaterialInventories(function(inventories) {
            self.currentMaterialInventory = null;
            for (var i = 0; i < inventories.length; i++) {
                if (inventories[i].Id === inventoryId) {
                    self.currentMaterialInventory = inventories[i];
                    break;
                }
            }

            if (!self.currentMaterialInventory) {
                throw new Error("Unknown MaterialInventory.Id");
            }

            self.searchMaterials();
        });

    };
};

app.virtualProductsEditor.vm = app.virtualProductsEditor.vm || new app.virtualProductsEditor.ViewModel();