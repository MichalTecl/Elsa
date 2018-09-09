var app = app || {};
app.virtualProductsEditor = app.virtualProductsEditor || {};
app.virtualProductsEditor.ViewModel = app.virtualProductsEditor.ViewModel || function() {

    var self = this;

    self.selectedMappables = [];

    var receiveMappables = function(mappables) {
        self.selectedMappables = mappables;
    };

    self.loadMappables = function(searchQuery) {
        lt.api("/virtualProducts/getMappableItems").query({ "searchQuery": searchQuery }).get(receiveMappables);
    };

};

app.virtualProductsEditor.vm = app.virtualProductsEditor.vm || new app.virtualProductsEditor.ViewModel();