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

    self.selectStep = function(stepId) {
        self.selectedStep = stepId;
        lt.notify();
    };

    setTimeout(loadAllMaterials, 0);
};

app.productionSteps.vm = app.productionSteps.vm || new app.productionSteps.ViewModel();
