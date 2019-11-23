var app = app || {};
app.recipesEditor = app.recipesEditor || {};
app.recipesEditor.VM = app.recipesEditor.VM || function() {

    var self = this;

    this.openRecipe = null;

    var alignRecipe = function() {
        if (!self.openRecipe) {
            return;
        }

        for (var i = 0; i < self.openRecipe.Items.length; i++) {
            self.openRecipe.Items[i].index = i;
        }
    };

    this.editRecipe = function(materialId, recipeId) {

        app.productionService.vm.cancelProducingRecipe();

        lt.api("/productionService/loadRecipe").query({ "materialId": materialId, "recipeId": recipeId })
            .get(function(received) {
                self.openRecipe = received;
                alignRecipe();
            });
    };

    this.cloneRecipe = function (materialId, recipeId) {

        app.productionService.vm.cancelProducingRecipe();

        lt.api("/productionService/loadRecipe").query({ "materialId": materialId, "recipeId": recipeId })
            .get(function (received) {
                received.RecipeId = 0;
                self.openRecipe = received;
                alignRecipe();
            });
    };


    this.toggleTransformationSource = function(index) {

        if (!self.openRecipe) {
            return;
        }

        var toUp = 0;
        for (var i = 0; i < self.openRecipe.Items.length; i++) {

            var component = self.openRecipe.Items[i];
            
            if (component.index === index) {
                component.IsTransformationSource = !component.IsTransformationSource;

                if (component.IsTransformationSource) {
                    toUp = i;
                }

            } else {
                component.IsTransformationSource = false;
            }
        }

        if (toUp > 0) {
            var mainComponent = self.openRecipe.Items[toUp];
            self.openRecipe.Items.splice(toUp, 1);
            self.openRecipe.Items.splice(0, 0, mainComponent);
        }
    };

    this.deleteComponent = function(index) {
        for (var i = 0; i < self.openRecipe.Items.length; i++) {
            var component = self.openRecipe.Items[i];
            if (component.index === index) {
                self.openRecipe.Items.splice(i, 1);
                return;
            }
        }
    };

    this.addComponent = function() {
        if (!self.openRecipe) {
            return;
        }

        var maxIndex = -1;

        for (var i = 0; i < self.openRecipe.Items.length; i++) {
            if (self.openRecipe.Items[i].index > maxIndex) {
                maxIndex = self.openRecipe.Items[i].index;
            }
        }

        self.openRecipe.Items.push({
            "IsTransformationSource": false,
            "Text": "",
            "IsValid": false,
            "Error": null,
            "index": maxIndex + 1
        });
    };

    this.moveComponent = function(index, direction) {

        if (!self.openRecipe) {
            return;
        }

        var j = 0;
        var item = null;
        for (; j < self.openRecipe.Items.length; j++) {
            if (self.openRecipe.Items[j].index === index) {
                item = self.openRecipe.Items[j];
                self.openRecipe.Items.splice(j, 1);
                break;
            }
        }

        if (!item) {
            return;
        }

        j = j + direction;

        if (j < 0) {
            j = 0;
        }

        self.openRecipe.Items.splice(j, 0, item);
    };

    this.cancelRecipeEdit = function() {
        self.openRecipe = null;
        lt.notify();
    };

    this.saveRecipe = function() {
        lt.api("/productionService/saveRecipe").body(self.openRecipe).post(function() {
            self.cancelRecipeEdit();
            app.productionService.vm.reloadRecipes();
        });
    };
};

app.recipesEditor.vm = app.recipesEditor.vm || new app.recipesEditor.VM();