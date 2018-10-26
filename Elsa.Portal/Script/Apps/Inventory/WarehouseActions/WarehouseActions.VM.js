var app = app || {};
app.warehouseActions = app.warehouseActions || {};
app.warehouseActions.ViewModel = app.warehouseActions.ViewModel || function() {

    var self = this;

    var allMaterialNames = null;

    var materialCallbacks = [];

    self.canLoadOlderBottomMaterialBatches = false;
    self.bottomMaterialBatches = [];
    var oldestBottomMaterialBatchTime = null;

    var receiveBottomMaterialBatch = function(entity) {

        var isNew = true;
        for (var i = 0; i < self.bottomMaterialBatches.length; i++) {
            if (self.bottomMaterialBatches[i].Id === entity.Id) {
                self.bottomMaterialBatches[i] = entity;
                isNew = false;
                break;
            }
        }

        if (oldestBottomMaterialBatchTime === null || entity.SortDt < oldestBottomMaterialBatchTime) {
            oldestBottomMaterialBatchTime = entity.SortDt;
        }

        if (isNew) {
            self.bottomMaterialBatches.push(entity);
        }
    };

    var sortBottomMaterialBatches = function() {
        self.bottomMaterialBatches.sort(function (a, b) { return b.SortDt > a.SortDt; });
        self.setBottomMaterialBatchEditMode();
    };

    self.setBottomMaterialBatchEditMode = function (batchId) {
        
        for (var i = self.bottomMaterialBatches.length - 1; i >= 0; i--) {
            
            if (self.bottomMaterialBatches[i].Id < 1) {
                self.bottomMaterialBatches.splice(i, 1);
                continue;
            }

            var e = self.bottomMaterialBatches[i];
            e.editMode = e.Id === batchId;
        }

        lt.notify();
    };
    
    self.searchMaterialNames = function(query, callback) {
        if (allMaterialNames) {
            callback(allMaterialNames);
            return;
        }

        materialCallbacks.push(callback);
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

    self.createBottomMaterialBatch = function() {
        
        sortBottomMaterialBatches();
        var newBatch = { "DisplayDt": "AUTO", "SortDt": 999999999999999999, "editMode": true, "hideDate": true, Id:0 };
        self.bottomMaterialBatches.unshift(newBatch);

        

        lt.notify();
    };

    self.loadBottomMaterialBatches = function() {

        lt.api("/warehouseActions/getBottomMaterialBatches")
            .query({ "before": oldestBottomMaterialBatchTime })
            .get(function(batches) {
                self.canLoadOlderBottomMaterialBatches = (batches && batches.length > 0);

                for (var i = 0; i < batches.length; i++) {
                    receiveBottomMaterialBatch(batches[i]);
                }

                sortBottomMaterialBatches();
            });
    };

    setTimeout(loadMaterialNames, 0);
    setTimeout(self.loadBottomMaterialBatches, 100);
};

app.warehouseActions.vm = app.warehouseActions.vm || new app.warehouseActions.ViewModel();