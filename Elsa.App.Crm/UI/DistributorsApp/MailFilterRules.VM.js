var app = app || {};
app.CrmMailFilterRules = app.CrmMailFilterRules || {};

app.CrmMailFilterRules.createListVm = function (config) {
    var vm = {};

    vm.rows = [];
    vm.isLoading = false;

    var snapshot = function (row) {
        return JSON.stringify(config.snapshot(row));
    };

    var normalizeRow = function (row) {
        row = row || {};
        row.RowKey = row.RowKey || row.Id || ("new_" + Math.random().toString(36).substring(2));
        row._originalState = snapshot(row);
        row.isDirty = false;
        return row;
    };

    var refreshDirty = function (row) {
        row.isDirty = row._originalState !== snapshot(row);
    };

    var receiveRows = function (rows) {
        vm.rows = (rows || []).map(normalizeRow);
        lt.notify();
    };

    vm.load = function () {
        vm.isLoading = true;
        lt.notify();

        lt.api(config.loadUrl).get(function (rows) {
            vm.isLoading = false;
            receiveRows(rows);
        });
    };

    vm.addRow = function () {
        var row = normalizeRow(config.emptyRowFactory());
        row.isDirty = true;
        vm.rows.unshift(row);
        lt.notify();
    };

    vm.updateField = function (row, field, value) {
        row[field] = value;
        refreshDirty(row);
        lt.notify();
    };

    vm.saveRow = function (row) {
        if (!row.isDirty)
            return;

        vm.isLoading = true;
        lt.notify();

        lt.api(config.saveUrl)
            .body(config.toPayload(row))
            .post(function (rows) {
                vm.isLoading = false;
                receiveRows(rows);
            });
    };

    vm.deleteRow = function (row) {
        if (!!row.Id && !window.confirm("Opravdu smazat?"))
            return;

        if (!row.Id) {
            vm.rows = vm.rows.filter(function (r) { return r.RowKey !== row.RowKey; });
            lt.notify();
            return;
        }

        vm.isLoading = true;
        lt.notify();

        lt.api(config.deleteUrl)
            .query({ id: row.Id })
            .post(function (rows) {
                vm.isLoading = false;
                receiveRows(rows);
            });
    };

    vm.load();
    return vm;
};
