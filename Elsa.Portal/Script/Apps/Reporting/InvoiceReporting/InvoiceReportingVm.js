var app = app || {};
app.invoiceReporting = app.invoiceReporting || {};
app.invoiceReporting.ViewModel = app.invoiceReporting.ViewModel || function() {

    var self = this;

    self.invoiceReportTypes = null;
    self.reportMonths = null;
    self.collection = null;

    var year = null;
    var month = null;
    var dataMethod = null;
    var generatorMethod = null;

    var loadReportTypes = function() {
        lt.api("/invoiceForms/GetInvoicingReportTypes").get(function(result) {
            self.invoiceReportTypes = result;
        });
    };

    var loadReportMonths = function() {
        lt.api("/invoiceForms/getReportMonths").get(function(result) {
            self.reportMonths = result;
        });
    };

    loadReportTypes();
    loadReportMonths();

    var load = function() {

        self.collection = null;

        if (!dataMethod) {
            return;
        }

        if ((year === null) || (month === null)) {
            return;
        }

        lt.api(dataMethod).query({ "year": year, "month": month }).get(function(data) {
            self.collection = data;
        });
    };

    self.selectMonth = function(newMonth, newYear) {
        month = newMonth;
        year = newYear;
        load();
    };

    self.setMethods = function(queryMethod, generateMethod) {
        year = null;
        month = null;
        dataMethod = queryMethod;
        generatorMethod = generateMethod;
    };

    self.requestGeneration = function() {
        if (!self.collection) {
            return;
        }

        lt.api(generatorMethod).query({
            "type": self.collection.InvoiceFormTypeId,
            "year": self.collection.Year,
            "month": self.collection.Month
        }).get(function(coll) {
            self.collection = coll;
        });
    };

    self.approveWarnings = function(ids) {
        lt.api("/invoiceForms/approveLogWarnings").body(ids).post(function() {
            load();
        });
    };

    self.deleteCollection = function() {
        lt.api("/invoiceForms/deleteCollection").query({ "id": self.collection.Id }).get(function() {
            load();
        });
    };

    self.approveCollection = function() {
        lt.api("/invoiceForms/approveCollection").query({ "id": self.collection.Id }).get(function () {
            load();
        });
    };
};

app.invoiceReporting.vm = app.invoiceReporting.vm || new app.invoiceReporting.ViewModel();