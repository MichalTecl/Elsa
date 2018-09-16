var app = app || {};
app.ui = app.ui || {};
app.ui.PickItemDialog = app.ui.PickItemDialog || function() {
    var self = this;

    var dialog = null;
    var title = "Use setTitle method";
    var multiMode = false;
    var shown = false;
    var items = [];
    var itemTemplate = document.createElement("DIV");
    itemTemplate.innerHTML = "ItemTemplate not set";

    var onClose = function() {};

    var onSearch = null;

    self.setTitle = function(t) {
        if (!!dialog) {
            dialog.setTitle(t);
        }

        title = t;
    };

    self.setMultiMode = function(value) {
        if (!!dialog) {
            dialog.setMultiMode(value);
        }

        multiMode = value;
    };

    self.show = function(callback) {
        if (!!dialog) {
            dialog.show(callback);
        }

        onClose = callback;

        shown = true;
    };

    self.close = function() {
        if (!!dialog) {
            dialog.close();
        }

        shown = false;
    };

    self.setOnSearch = function(callback) {
        if (!!dialog) {
            dialog.setOnSearch(callback);
        }

        onSearch = callback;
    };

    self.setItems = function(source) {
        if (!!dialog) {
            dialog.setItems(source);
        }

        items = source;
    };

    self.setItemTemplate = function(value) {
        if (!!dialog) {
            dialog.setItemTemplate(value);
        }

        itemTemplate = value;
    };

    self.getSelectedItems = function() {
        if (!dialog) {
            return [];
        }

        return dialog.getSelectedItems();
    };

    var existingDialog = document.getElementById("pickItemDialog");
    if (!!existingDialog) {
        dialog = existingDialog;
    } else {

        var container = document.createElement("DIV");
        document.body.appendChild(container);

        lt.replaceBy(container,
            "/UI/Controls/Common/PickItemDialog.html",
            function(d) {
                dialog = d;
                self.setTitle(title);
                self.setMultiMode(multiMode);
                self.setItemTemplate(itemTemplate);
                self.setItems(items);
                self.setOnSearch(onSearch);

                if (shown) {
                    self.show(onClose);
                } else {
                    self.close();
                }
            });
    }
};