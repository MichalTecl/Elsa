var app = app || {};
app.invoiceReporting = app.invoiceReporting || {};
app.invoiceReporting.ViewModel = app.invoiceReporting.ViewModel || function() {

    var self = this;

    self.invoiceReportTypes = null;
    self.reportMonths = null;
    self.data = [];

    var year = null;
    var month = null;
    var dataMethod = null;

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

        self.data = [];

        if (!dataMethod) {
            return;
        }

        if ((year === null) || (month === null)) {
            return;
        }

        lt.api(dataMethod).query({ "year": year, "month": month }).get(function(data) {
            self.data = data;
        });
    };

    self.selectMonth = function(newMonth, newYear) {
        month = newMonth;
        year = newYear;
        load();
    };

    self.setQueryMethod = function(method) {
        year = null;
        month = null;
        dataMethod = method;
    };
};

app.invoiceReporting.vm = app.invoiceReporting.vm || new app.invoiceReporting.ViewModel();