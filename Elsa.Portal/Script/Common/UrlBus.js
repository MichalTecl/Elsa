var app = app || {};
app.UrlBus = app.UrlBus || function() {

    var currentJsonValues = {};
    var currentObjValues = {};
    var callbacks = {};

    var self = this;

    var changeHandler = function() {
        const srch = new URLSearchParams(window.location.search);

        var htext = window.location.hash;
        if (htext.indexOf("#") === 0) {
            htext = htext.substr(1);
        }

        const hash = new URLSearchParams(htext);

        const all = {};

        for (let key of srch.keys()) {
            all[key] = srch.get(key);
        }

        for (let key of hash.keys()) {
            var trgKey = key;
            if (trgKey.indexOf("#") === 0) {
                trgKey = trgKey.substr(1);
            }
            all[trgKey] = hash.get(key);
        }

        var acknowledgeds = [];

        for (let key in all) {
            const old = currentJsonValues[key];
            const nw = all[key];
            if (old === nw) {
                continue;
            }

            currentJsonValues[key] = nw;
            if (nw == null) {
                currentObjValues[key] = null;
                continue;
            }

            try {
                currentObjValues[key] = JSON.parse(nw);
            } catch (e) {
                currentObjValues[key] = currentJsonValues[key];
            }

            let gotAck = false;
            const cbks = callbacks[key];
            if (cbks != null) {
                for (var cbkid = 0; cbkid < cbks.length; cbkid++) {
                    var cbk = cbks[cbkid];
                    gotAck = cbk(currentObjValues[key]) | gotAck;
                }
            }

            if (gotAck) {
                acknowledgeds.push(key);
            }
        }

        for (var acix = 0; acix < acknowledgeds.length; acix++) {
            self.clear(acknowledgeds[acix]);
        }
    };
    

    var init = function () {

        if ((!document) || (!document.body)) {
            setTimeout(init, 100);
            return;
        }

        changeHandler();
        window.addEventListener("hashchange", changeHandler, false);
    };

    init();

    self.watch = function(param, callback) {

        var current = currentObjValues[param];
        if (current != null) {
            if (callback(current) === true) {
                self.clear(param);
            }
        }

        (callbacks[param] = callbacks[param] || []).push(callback);
    };

    self.set = function(param, value, keepThisTab) {
        var setter = new URLSearchParams(window.location.hash);

        if (value == null) {
            delete currentJsonValues[param];
            delete currentObjValues[param];
        }

        if (setter.has("#" + param)) {
            param = "#" + param;
        }

        if (value == null) {
            delete currentJsonValues[param];
            delete currentObjValues[param];
            setter.delete(param);
        } else {
            setter.set(param, JSON.stringify(value));
        }

        try {
            history.replaceState(null, null, ' ');
        } catch(e){}

        if (keepThisTab) {
            window.location.hash = setter.toString();
        } else {
            setter.set("elNewTab", "yes");
            window.open(window.location.href + "#" + setter.toString(), "_blank");
        }
    };

    self.clear = function (param) {

        if (window.location.href.indexOf("elNewTab=yes") > -1) {
            window.close();
            return;
        }

        self.set(param, null);
    };

    self.get = function(param) {
        return currentObjValues[param];
    };
};


app.urlBus = app.urlBus || new app.UrlBus();
