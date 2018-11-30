var app = app || {};
app.MaterialHelper = app.MaterialHelper || function() {

    var self = this;

    var namedIndex = {};

    var loadMaterials = function() {

        lt.api("/material/GetAllMaterialInfo").get(function(info) {
            for (var i = 0; i < info.length; i++) {
                var entity = info[i];

                namedIndex[entity.MaterialName] = entity;
            }
        });
    };

    var raiseChanged = function(inp) {
        if ("createEvent" in document) {
            var evt = document.createEvent("HTMLEvents");
            evt.initEvent("change", false, true);
            inp.dispatchEvent(evt);
        } else {
            inp.fireEvent("onchange");
        }
    };

    setTimeout(loadMaterials, 0);

    self.getMaterialInfoByName = function(name) {
        return namedIndex[name];
    };

    self.autofill = function(tbMaterial, tbUnit, tbBatch, onComplete) {
        if ((!tbMaterial) || (!tbMaterial.value)) {
            onComplete();
            return;
        }

        var indexed = namedIndex[tbMaterial.value];
        if (!indexed) {
            onComplete();
            return;
        }

        var receive = function(info) {
            if (tbUnit && (tbUnit.value || "").length === 0) {
                tbUnit.value = info.PreferredUnitSymbol;
                raiseChanged(tbUnit);
            }

            if (tbBatch && (tbBatch.value || "").length === 0 && info.AutoBatchNr) {
                tbBatch.value = info.AutoBatchNr;
                raiseChanged(tbBatch);
            }

            onComplete();
        };

        if ((!tbBatch) || (!indexed.AutoBatchNr)) {
            receive(indexed);
            return;
        }

        lt.api("/material/GetMaterialInfo").query({ "materialName": materialName }).get(function(m) {
            namedIndex[m.MaterialName] = m;
            receive(m);
        });
    };
};

app.materialHelper = app.materialHelper || new app.MaterialHelper();