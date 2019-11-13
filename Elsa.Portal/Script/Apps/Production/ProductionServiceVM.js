var app = app || {};
app.productionService = app.productionService || {};
app.productionService.VM = app.productionService.VM || function() {

    var self = this;

    this.producingRecipe = null;

    this.onlyFavorite = true;
    this.showDeleted = false;
    this.searchFilter = "";

    this.recipes = [];


    this.uploadProducingRecipe = function() {

        if (!self.producingRecipe) {
            return;
        }

        lt.api("/productionService/ValidateProductionRequest").body(self.producingRecipe)
            .post(function(received) {
                self.producingRecipe = received;
            });
    };

    self.setProducingRecipe = function(id) {

        self.producingRecipe = self.producingRecipe || {};
        self.producingRecipe.RecipeId = id;

        self.uploadProducingRecipe();
    };
    
    var updateRecipesView = function() {

        var textMatcher = new TextMatcher(self.searchFilter);

        for (var matnodeIndex = 0; matnodeIndex < self.recipes.length; matnodeIndex++) {
            var materialNode = self.recipes[matnodeIndex];
            materialNode.Visible = false;

            for (var recindex = 0; recindex < materialNode.Recipes.length; recindex++) {
                var recipe = materialNode.Recipes[recindex];
                recipe.Visible = true;

                if (!textMatcher.match(recipe.RecipeName, true)) {
                    recipe.Visible = false;
                }

                if ((!self.showDeleted) && (!recipe.IsActive)) {
                    recipe.Visible = false;
                }

                if (self.onlyFavorite && (!recipe.IsFavorite)) {
                    recipe.Visible = false;
                }

                materialNode.Visible = materialNode.Visible || recipe.Visible;
            }
        }
    };
    
    var getOrCreateMaterialNode = function(materialId, materialName) {
    
        for (var i = self.recipes.length - 1; i >= 0; i--) {
            var node = self.recipes[i];

            if (node.MaterialId === materialId) {
                return node;
            }
        }

        var nnode = { "MaterialId": materialId, "MaterialName": materialName, "Visible": true, "Recipes":[] };
        self.recipes.push(nnode);

        return nnode;
    };

    this.updateFilters = function(onlyFavorite, showDeleted, searchFilter) {

        self.onlyFavorite = onlyFavorite;
        self.showDeleted = showDeleted;
        self.searchFilter = searchFilter;

        updateRecipesView();

        lt.notify();
    };

    var receiveUpdatedRecipe = function(updatedRecipe) {
        var materialNode = getOrCreateMaterialNode(updatedRecipe.MaterialId);

        for (var i = 0; i < materialNode.Recipes.length; i++) {
            var recipe = materialNode.Recipes[i];
            if (recipe.RecipeId === updatedRecipe.RecipeId) {
                recipe.IsFavorite = updatedRecipe.IsFavorite;
                break;
            }
        }

        updateRecipesView();
    };

    this.updateComponentAmount = function(key, amount) {

        if ((!self.producingRecipe) || (!self.producingRecipe.Components)) {
            return;
        }

        for (var cid = 0; cid < self.producingRecipe.Components.length; cid++) {
            var component = self.producingRecipe.Components[cid];

            for (var rid = 0; rid < component.Resolutions.length; rid++) {
                var resolution = component.Resolutions[rid];
                if (resolution.Key === key) {
                    resolution.Amount = amount;
                    return;
                }
            }
        }

        console.warn("No resolution found by Key=" + key);
    };

    this.resetAllocations = function(materialId) {

        for (var cid = 0; cid < self.producingRecipe.Components.length; cid++) {
            var component = self.producingRecipe.Components[cid];
            if (component.MaterialId === materialId) {
                component.Resolutions = [];
                self.uploadProducingRecipe();
                return;
            }
        }
    };

    this.cancelProducingRecipe = function() {
        self.producingRecipe = null;
        lt.notify();
    };

    this.saveProducingRecipe = function() {
        lt.api("/productionService/ProcessProductionRequest").body(self.producingRecipe)
            .post(function (received) {
                location.reload();
            });
    };

    this.toggleFavorite = function (recipeId) {
        lt.api("/productionService/toggleFavorite").query({ "recipeId": recipeId }).get(receiveUpdatedRecipe);
    };

    var loadRecipes = function() {

        self.recipes = [];
        lt.notify();

        lt.api("/productionService/getRecipes").get(function(received) {

            var hasFavorite = false;

            for (var rindex = 0; rindex < received.length; rindex++) {
                var recipe = received[rindex];
                recipe.Visible = true;

                hasFavorite = hasFavorite || recipe.IsFavorite;

                var materialNode = getOrCreateMaterialNode(recipe.MaterialId, recipe.MaterialName);
                materialNode.Recipes.push(recipe);
            }

            self.onlyFavorite = hasFavorite;

            updateRecipesView();
        });

    };
    setTimeout(loadRecipes, 0);
};

app.productionService.vm = app.productionService.vm || new app.productionService.VM();