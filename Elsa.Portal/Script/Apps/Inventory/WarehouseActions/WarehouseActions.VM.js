var app = app || {};
app.warehouseActions = app.warehouseActions || {};
app.warehouseActions.ViewModel = app.warehouseActions.ViewModel || function() {

    var self = this;

    var allMaterialNames = null;

    var materialCallbacks = [];

    self.materialStockEvents = [];
    var lastLoadedStockEventTime = null;

    self.searchMaterialNames = function(query, callback) {
        if (!!allMaterialNames) {
            callback(allMaterialNames);
            return;
        }

        materialCallbacks.push(callback);
    };

    var receiveMaterialStockEvent = function(event) {

        var isNew = true;
        for (var i = 0; i < self.materialStockEvents.length; i++) {
            if (self.materialStockEvents[i].Id === event.Id) {
                self.materialStockEvents[i] = event;
                isNew = false;
                break;
            }
        }
        
        if ((lastLoadedStockEventTime === null) || (lastLoadedStockEventTime > event.Time)) {
            lastLoadedStockEventTime = event.Time;
        }

        if (isNew) {
            self.materialStockEvents.push(event);
        }
    };

    var sortStockEvents = function() {
        self.materialStockEvents.sort(function(a, b) {
            return b.Time - a.Time;
        });
    };

    self.saveWarehouseFillAction = function(materialName, volume, unit, description, savedCallback) {
        lt.api("/warehouseActions/SaveWarehouseFillEvent")
            .body({ "materialName": materialName, "amount": volume, "unitName": unit, "note": description })
            .post(function(entity) {
                savedCallback();
                receiveMaterialStockEvent(entity);
                sortStockEvents();
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

                if (lastLoadedStockEventTime === null) {
                    setTimeout(self.loadMaterialStockEvents, 0);
                }
            });
    };

    self.loadMaterialStockEvents = function() {
        lt.api("/warehouseActions/GetMaterialStockEvents").query({ "lastSeenTime": lastLoadedStockEventTime }).get(function(events) {
            for (var i = 0; i < events.length; i++) {
                var event = events[i];
                receiveMaterialStockEvent(event);
            }

            sortStockEvents();
        });
    };

    setTimeout(loadMaterialNames, 0);
    
};

app.warehouseActions.vm = app.warehouseActions.vm || new app.warehouseActions.ViewModel();