var app = app || {};
app.productionSteps = app.productionSteps || {};
app.productionSteps.ViewModel = app.productionSteps.ViewModel || function() {

    var self = this;

    self.allMaterials = [];
    self.selectedStep = null;
    
    var loadAllMaterials = function() {
        lt.api("/productionSteps/getAllMaterialsWithSteps").get(function(materials) {
            self.allMaterials = materials;
        });
    };

    var receiveSelectedStep = function(model) {

        model.hasComponents = model.Materials.length > 0;

        for (var i = 0; i < model.Materials.length; i++) {
            var entry = model.Materials[i];
            entry.itemKey = entry.MaterialId + "_" + (entry.BatchNumber || "?");
            entry.cannotResolve = entry.AutomaticBatches && ((entry.BatchNumber || "").trim().length < 1);
        }
        
        self.selectedStep = model;
    };

    self.selectStep = function (stepId, batchNr) {
        
        for (var i = 0; i < self.allMaterials.length; i++) {
            var material = self.allMaterials[i];

            for (var j = 0; j < material.Steps.length; j++) {
                var step = material.Steps[j];
                if (step.MaterialProductionStepId !== stepId) {
                    continue;
                }

                var request = {
                    MaterialProductionStepId: stepId,
                    MaterialId: material.MaterialId
                };

                lt.api("/productionSteps/validate").body(request).post(receiveSelectedStep);
                return;
            }
        }

        throw new Error("Record not found");
    };

    var validateStep = function() {
        lt.api("/productionSteps/validate").body(self.selectedStep).post(receiveSelectedStep);
    };

    self.setBatchNumber = function(batchNr) {
        self.selectedStep.BatchNumber = batchNr;
        validateStep();
    };

    self.cancelStepEdit = function() {
        self.selectedStep = null;
        lt.notify();
    };

    self.updateQuantity = function(qty) {
        self.selectedStep.Quantity = qty;
        validateStep();
    };

    self.updateComponent = function(itemKey, batchNr, amount) {
        
        for (var i = 0; i < self.selectedStep.Materials.length; i++) {
            var mat = self.selectedStep.Materials[i];

            if (mat.itemKey !== itemKey) {
                continue;
            }

            mat.BatchNumber = batchNr;
            mat.Amount = amount;
            break;
        }

        lt.notify();

        validateStep();
    };

    self.updateAdditionalFields = function(worker, time, price) {

        self.selectedStep.Worker = worker;
        self.selectedStep.Price = price;
        self.selectedStep.Hours = time;

        lt.notify();

        validateStep();
    };

    self.doStepValidation = validateStep;

    self.saveStep = function() {
        lt.api("/productionSteps/saveProductionStep").body(self.selectedStep).post(function() {
            self.cancelStepEdit();
        });
    };

    setTimeout(loadAllMaterials, 0);
};

app.productionSteps.vm = app.productionSteps.vm || new app.productionSteps.ViewModel();
