var app = app || {};
app.fixedCosts = app.fixedCosts || {};
app.fixedCosts.ViewModel = app.fixedCosts.ViewModel || function() {
    var self = this;

    self.months = null;
    self.values = [];
    self.canSave = false;

    self.selectMonth = function (year, month) {
        self.canSave = false;
        self.values = [];
        lt.notify();

        lt.api("/fixedCostTypes/getValues").query({ "month": month, "year": year }).get(
            function (data) {

                for (var i = 0; i < data.length; i++) {
                    data[i].isChanged = false;
                }

                self.values = data;
            }
        );
    };

    self.setValue = function(vm, value) {

        if (vm.Value === value) {
            return;
        }

        vm.Value = value;
        vm.isChanged = true;
        self.canSave = true;

        lt.notify();
    };

    var saveItem = function() {

        for (var i = 0; i < self.values.length; i++) {

            var value = self.values[i];
            if (!value.isChanged) {
                continue;
            }

            value.isChanged = false;

            lt.api("/fixedCostTypes/setValue").body(value).post(function(done) {
                saveItem();
                return;
            });
        }

        self.canSave = false;
        lt.notify();
    };

    self.save = function() {
        saveItem();
    };

    var init = function() {

        if (!lt || !lt.api) {
            setTimeout(init, 100);
            return;
        }

        lt.api("/fixedCostTypes/getMonths").get(function(months) {
            self.months = months;
        });
    };

    init();
};

app.fixedCosts.vm = app.fixedCosts.vm || new app.fixedCosts.ViewModel();