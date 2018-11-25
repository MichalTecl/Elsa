var app = app || {};
app.production = app.production || {};
app.production.ViewModel = app.production.ViewModel || function() {

    var self = this;
    var oldestBatchDt = null;

    self.producedBatches = [];

    self.editBatch = null;

    var receiveBatch = function(batch) {};

    self.openBatch = function(batchId) {
        lt.api("/production/getProductionBatch").query({ "batchId": batchId }).get(function(batch) {
            receiveBatch(batch);
            self.editBatch = batch;
        });
    };

    self.newBatch = function() {
        var nb = {
            "isHeadEdit": true
        };
        receiveBatch(nb);

        self.editBatch = nb;
        lt.notify();
    };

    self.cancelEdit = function() {
        self.editedBatch = null;
        lt.notify();
    };

    self.checkHeadChanged = function(material, amount, unit, batchNr) {
        if (!self.editBatch) {
            return;
        }

        amount = amount || "0";

        var newValue = (
            self.editBatch.MaterialName !== material ||
            self.editBatch.ProducedAmount !== parseFloat(amount) ||
            self.editBatch.ProducedAmountUnitSymbol !== unit ||
            self.editBatch.BatchNumber !== batchNr);
        
        if (self.editBatch.isHeadEdit === newValue) {
            return;
        }

        self.editBatch.isHeadEdit = newValue;

        lt.notify();
    };

    self.setBatchHead = function(materialName, batchNumber, amount, unit) {

        var request = {
            "BatchId": self.editBatch.BatchId,
            "MaterialName": materialName,
            "BatchNumber": batchNumber,
            "Amount": amount,
            "AmountUnitSymbol": unit
        };

        lt.api("/production/createBatch").body(request).post(function(batch) {
            self.editBatch = batch;
        });
    };

    self.loadBatches = function() {
        lt.api("/production/getBatches").query({ "lastSeen": oldestBatchDt }).get(function(batches) {
            
            for (var i = 0; i < batches.length; i++) {
                var b = batches[i];
                if (oldestBatchDt === null || oldestBatchDt > b.PagingDt) {
                    oldestBatchDt = b.PagingDt;
                }

                self.producedBatches.push(b);
            }

        });
    };

    self.loadBatches();
};

app.production.vm = app.production.vm || new app.production.ViewModel();