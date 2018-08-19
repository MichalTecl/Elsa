var app = app || {};
app.widgets = app.widgets || {};
app.widgets.ViewModel = app.widgets.ViewModel || function() {

    var self = this;

    self.widgets = [];

    var update = function() {
        lt.api("/widgets/getWidgets").ignoreDisabledApi().get(function(widgets) {
            self.widgets = widgets;
        });
    };

    app.user.vm.subscribeUserChange(update);
    update();
};

app.widgets.vm = new app.widgets.ViewModel();
