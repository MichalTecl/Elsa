var app = app || {};
app.virtualProductsEditor = app.virtualProductsEditor || {};
app.virtualProductsEditor.ViewModel = app.virtualProductsEditor.ViewModel || function() {

    var self = this;

    self.selectedMappables = [];
    self.currentQuery = "";

    self.selectedVirtualProducts = [];
    self.selectedMaterials = [];
    
    var adjustServerMaterial = function(mat) {

        mat.editMode = false;

        var materials = [];

        for (var i = 0; i < mat.Components.length; i++) {

            var com = mat.Components[i];

            materials.push({ Name: com.Material.Name, Amount: com.Amount, UnitSymbol: com.Unit.Symbol });
        }
        mat.materials = materials;

        mat.nominalAmountText = mat.NominalAmount + mat.NominalUnit.Symbol;
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
            var newVp = { editMode: true, materials:[] };
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

    self.searchMaterials = function(query) {
        lt.api("/virtualProducts/searchMaterials").body(query).post(receiveMaterials);
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
            self.selectedMaterials.unshift({ editMode: true, materials: [] });
        }

        lt.notify();
    };

    self.saveMaterial = function(model) {
        var request = {
            MaterialId: model.Id,
            MaterialName: model.Name,
            NominalAmountText: model.nominalAmountText,
            Materials: []
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


    setTimeout(self.searchMaterials, 500);
    setTimeout(self.loadVirtualProducts, 100);
};

app.virtualProductsEditor.vm = app.virtualProductsEditor.vm || new app.virtualProductsEditor.ViewModel();