var app = app || {};
app.production = app.production || {};
app.production.ViewModel = app.production.ViewModel || function() {

    var self = this;
    var oldestBatchDt = null;
    
    self.producedBatches = [];

    self.editBatch = null;

    var receiveBatch = function(batch) {

        var assignmentIndex = 0;

        if (batch.Components) {
            for (var i = 0; i < batch.Components.length; i++) {
                var assignments = batch.Components[i].Assignments;

                if (assignments) {
                    for (var j = 0; j < assignments.length; j++) {
                        assignments[j].assignmentIndex = assignmentIndex;
                        assignments[j].isDirty = false;
                        assignments[j].isDisabled = false;
                        assignments[j].parentMaterialId = batch.Components[i].MaterialId;
                        assignmentIndex++;
                    }
                }
            }
        }

    };

    var getAssignment = function(index) {
        var batch = self.editBatch;
        if (!batch || !batch.Components) {
            return null;
        }
        
        for (var i = 0; i < batch.Components.length; i++) {
            var assignments = batch.Components[i].Assignments;

            if (assignments) {
                for (var j = 0; j < assignments.length; j++) {
                    if (assignments[j].assignmentIndex === index) {
                        return assignments[j];
                    }
                }
            }
        }

        return null;
    };

    self.getAssignmentByIndex = function(index) {
        return getAssignment(index);
    };

    var crawlAssignments = function(callback) {
        var batch = self.editBatch;
        if (!batch || !batch.Components) {
            return null;
        }

        for (var i = 0; i < batch.Components.length; i++) {
            var assignments = batch.Components[i].Assignments;

            if (assignments) {
                for (var j = 0; j < assignments.length; j++) {
                    callback(assignments[j]);
                }
            }
        }
    };

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
            receiveBatch(batch);
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

    self.checkAssignmentChanged = function(assignmentIndex, amount, unit, batchNr) {
        var assignment = getAssignment(assignmentIndex);
        if (!assignment) {
            return;
        }

        var newStatus = ((assignment.UsedBatchNumber || "") !== (batchNr || "") ||
            parseFloat(assignment.UsedAmount) !== parseFloat(amount) ||
            (assignment.UsedAmountUnitSymbol || "") !== (unit || ""));

        var newIsValid = (parseFloat(amount || "0") > 0 && (unit || "").length > 0 && (batchNr || "").length > 0);

        if (newStatus === assignment.isDirty && newIsValid === assignment.isValid) {
            return;
        }
        
        assignment.isDirty = newStatus;
        assignment.isValid = newIsValid;

        crawlAssignments(function(a) {
            a.isDisabled = a.assignmentIndex !== assignmentIndex && newStatus;
        });
        
        lt.notify();
    };

    self.commitAssignmentChange = function(assignmentIndex, amount, unit, batchNr) {

        var assignment = getAssignment(assignmentIndex);
        if (!assignment) { return; }
        
        lt.api("/production/addComponentSourceBatch")
            .query({
                "materialBatchCompositionId": assignment.MaterialBatchCompositionId,
                "productionBatchId": self.editBatch.BatchId,
                "materialId": assignment.parentMaterialId,
                "sourceBatchNumber": batchNr,
                "usedAmount": amount,
                "usedAmountUnitSymbol": unit
            })
            .get(function(b) {
                receiveBatch(b);
                self.editBatch = b;
            });
    };

    self.loadBatches();
};

app.production.vm = app.production.vm || new app.production.ViewModel();