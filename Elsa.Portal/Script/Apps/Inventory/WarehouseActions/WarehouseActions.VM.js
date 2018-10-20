var app = app || {};
app.warehouseActions = app.warehouseActions || {};
app.warehouseActions.ViewModel = app.warehouseActions.ViewModel || function() {

    var self = this;

    var allMaterialNames = null;

    var materialCallbacks = [];

    self.searchMaterialNames = function(query, callback) {
        if (!!allMaterialNames) {
            callback(allMaterialNames);
            return;
        }

        materialCallbacks.push(callback);
    };

    self.saveWarehouseFillAction = function(materialName, volume, unit, description, savedCallback) {
        lt.api("/warehouseActions/SaveWarehouseFillEvent")
            .body({ "materialName": materialName, "amount": volume, "unitName": unit, "note": description })
            .post(function() {
                savedCallback();
            });
    };

    var loadMaterialNames = function() {
        lt.api("/virtualProducts/getAllMaterialNames")
            .get(function(names) {
                allMaterialNames = names;

                while (materialCallbacks.length > 0) {
                    var callback = materialCallbacks.shift();
                    callback(allMaterialNames);
                }

            });
    };

    setTimeout(loadMaterialNames, 0);
};

app.warehouseActions.vm = app.warehouseActions.vm || new app.warehouseActions.ViewModel();