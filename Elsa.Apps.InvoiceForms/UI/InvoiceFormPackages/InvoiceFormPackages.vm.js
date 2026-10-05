var app = app || {};
app.InvoiceFormPackages = app.InvoiceFormPackages || {};
app.InvoiceFormPackages.VM = app.InvoiceFormPackages.VM || function () {
    var self = this;
    self.collections = [];
    self.isEmpty = false;
    self.error = "";
    var isBusy = false;

    var receiveCollections = function (collections) {
        isBusy = false;
        self.collections = collections || [];
        self.isEmpty = self.collections.length === 0;
        self.error = "";
        lt.notify();
    };

    var onError = function () {
        isBusy = false;
        self.collections.forEach(function (collection) { collection.isBusy = false; });
        self.error = "Operace se nezdařila. Obnovte stránku a zkuste to znovu.";
        lt.notify();
    };

    self.deletePackage = function (collection) {
        if (isBusy || !collection || !collection.CanDelete)
            return;

        if (!window.confirm("Opravdu chcete smazat balíček účetních dat " + collection.Title + "? Smazaná data se znovu vygenerují v noci."))
            return;

        isBusy = true;
        collection.isBusy = true;
        self.error = "";
        lt.notify();

        lt.api("/invoiceFormPackages/deletePackage")
            .query({ publicUid: collection.PublicUid })
            .onerror(onError)
            .post(receiveCollections);
    };

    lt.api("/invoiceFormPackages/getCollections")
        .onerror(onError)
        .get(receiveCollections);
};
app.InvoiceFormPackages.vm = app.InvoiceFormPackages.vm || new app.InvoiceFormPackages.VM();