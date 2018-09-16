var app = app || {};
app.virtualProductsEditor = app.virtualProductsEditor || {};
app.virtualProductsEditor.ViewModel = app.virtualProductsEditor.ViewModel || function() {

    var self = this;

    self.selectedMappables = [];
    self.currentQuery = "";

    self.selectedVirtualProducts = [];
    
    var receiveMappables = function (mappables) {

        for (var i = 0; i < mappables.length; i++) {
            var mappable = mappables[i];

            for (var j = 0; j < mappable.AssignedVirtualProducts.length; j++) {
                mappable.AssignedVirtualProducts[j].ownerMappableId = JSON.parse(mappable.Id);
            }
        }

        self.selectedMappables = mappables;
    };

    var receiveVirtualProducts = function (vps) {

        /*for (var i = 0; i < vps.length; i++) {
            vps[i].editMode = false;
        }*/

        self.selectedVirtualProducts = vps;
        self.cancelVpEdit(true);
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

    setTimeout(self.loadVirtualProducts(""), 10);

    self.cancelVpEdit = function(doNotNotify) {

        if (self.selectedVirtualProducts.length > 0 && (!self.selectedVirtualProducts[0].VirtualProductId)) {
            self.selectedVirtualProducts.splice(0, 1);
        }

        for (var i = 0; i < self.selectedVirtualProducts.length; i++) {
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
            var newVp = { editMode: true };
            self.selectedVirtualProducts.unshift(newVp);
        }

        lt.notify();
    };
};

app.virtualProductsEditor.vm = app.virtualProductsEditor.vm || new app.virtualProductsEditor.ViewModel();