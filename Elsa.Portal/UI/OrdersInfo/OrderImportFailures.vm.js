var app = app || {};
app.OrderImportFailures = app.OrderImportFailures || {};

app.OrderImportFailures.VM = app.OrderImportFailures.VM || function () {
    var self = this;

    self.failures = [];
    self.isLoading = false;
    self.hasFailures = false;
    self.isEmpty = false;

    var formatDate = function (value) {
        var date = new Date(value);
        if (isNaN(date.getTime())) {
            return { text: "", title: "" };
        }

        return {
            text: date.toLocaleDateString("cs-CZ") + " " + date.toLocaleTimeString("cs-CZ", { hour: "2-digit", minute: "2-digit" }),
            title: date.toLocaleString("cs-CZ")
        };
    };

    var formatFailure = function (failure) {
        var firstFailureDt = formatDate(failure.FirstFailureDt);
        var lastFailureDt = formatDate(failure.LastFailureDt);
        var lastError = failure.LastError || "";

        failure.FirstFailureDtText = firstFailureDt.text;
        failure.FirstFailureDtTitle = firstFailureDt.title;
        failure.LastFailureDtText = lastFailureDt.text;
        failure.LastFailureDtTitle = lastFailureDt.title;
        failure.LastErrorSummary = lastError.split(/\r?\n/)[0];
        failure.IsRetrying = false;
        failure.RetryButtonText = "Zkusit znovu";
        failure.RetryError = null;
        failure.HasRetryError = false;

        return failure;
    };

    var receiveFailures = function (failures) {
        self.failures = (failures || []).map(formatFailure);
        self.hasFailures = self.failures.length > 0;
        self.isEmpty = !self.hasFailures;
        self.isLoading = false;
        lt.notify();
    };

    self.load = function () {
        self.isLoading = true;
        self.isEmpty = false;
        lt.notify();

        lt.api("/ordersInfo/getOrderImportFailures")
            .get(receiveFailures);
    };

    self.retry = function (failure) {
        if (!failure || failure.IsRetrying) {
            return;
        }

        failure.IsRetrying = true;
        failure.RetryButtonText = "Importuji…";
        failure.RetryError = null;
        failure.HasRetryError = false;
        lt.notify();

        lt.api("/ordersInfo/retryOrderImport")
            .query({ failureId: failure.Id })
            .onerror(function (error) {
                failure.IsRetrying = false;
                failure.RetryButtonText = "Zkusit znovu";
                failure.RetryError = error && error.message ? error.message : (error || "Opakovaný import selhal.");
                failure.HasRetryError = true;
                lt.notify();
            })
            .post(function () {
                self.load();
            });
    };
};

app.OrderImportFailures.vm = app.OrderImportFailures.vm || new app.OrderImportFailures.VM();
