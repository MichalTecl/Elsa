var app = app || {};
app.MaterialHelper = app.MaterialHelper || function() {

    var self = this;

    var namedIndex = {};
    var idIndex = {};

    var callbacks = {};
    var loaded = false;

    var loadMaterials = function() {

        lt.api("/material/GetAllMaterialInfo").get(function(info) {
            for (var i = 0; i < info.length; i++) {
                var entity = info[i];

                namedIndex[entity.MaterialName] = entity;
                idIndex[entity.MaterialId] = entity;

                var cbks = callbacks[entity.MaterialName];
                if (cbks != null) {
                    for (let ci = 0; ci < cbks.length; ci++) {
                        var fn = cbks[ci];
                        fn(entity);
                    }
                }
            }

            loaded = true;
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

    self.waitMaterialInfoByName = function(name, callback) {
        if (self.getMaterialInfoByName(name) != null) {
            callback(self.getMaterialInfoByName(name));
            return;
        }

        if (loaded) {
            callback(null);
            return;
        }

        (callbacks[name] = callbacks[name] || []).push(callback);
    };

    self.getMaterialInfoById = function(materialId) {
        if (materialId == null) {
            return null;
        }

        return idIndex[materialId];
    };

    self.autofill = function(tbMaterial, tbUnit, tbBatch, onComplete) {

        onComplete = onComplete || function() {};

        if ((!tbMaterial) || (!tbMaterial.value)) {
            onComplete(null);
            return;
        }

        var indexed = namedIndex[tbMaterial.value];
        if (!indexed) {
            onComplete(null);
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

            onComplete(info);
        };

        if ((!tbBatch) || (!indexed.AutoBatchNr)) {
            receive(indexed);
            return;
        }

        lt.api("/material/GetMaterialInfo").query({ "materialName": tbMaterial.value }).get(function(m) {
            namedIndex[m.MaterialName] = m;
            receive(m);
        });
    };
};

app.materialHelper = app.materialHelper || new app.MaterialHelper();