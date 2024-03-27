var app = app || {};
app.ImportExport = app.ImportExport || {};
app.ImportExport.VM = app.ImportExport.VM ||
    function () {
        var self = this;

        self.modules = [];

        lt.api("/importexport/getmodules").get(function (modules) {

            for (var module of modules) {
                module.downloadHref = "/importExport/export?moduleUid=" + module.Uid;
            }

            self.modules = modules;
        });
    };

app.ImportExport.vm = app.ImportExport.vm || new app.ImportExport.VM();