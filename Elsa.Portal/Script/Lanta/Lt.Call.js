var lanta = lanta || {};
lanta.ApiCallBuilder = lanta.ApiCallBuilder || function (url) {

    var self = this;
    var queryString = "";
    var bodyJson = null;
    var useCache = false;
    var noJson = false;
    var throughDisabled = false;
    var httpMethod = "get";

    var responseHandler = function (resp) {
        console.log("No response handler. Request=" + url + " response:" + resp);
        lt.notify();
    };

    var errorHandler = function (err) { lanta.Extensions.defaultErrorHandler(new Error(err)); };

    var call = function (method, handler) {

        httpMethod = method || httpMethod;
        responseHandler = handler || responseHandler;

        if (!lanta.ApiCallBuilder.isEnabled && !throughDisabled) {

            var selfUrl = self.getUrl();
            for (let i = 0; i < lanta.ApiCallBuilder.postponedCalls.length; i++) {
                var call = lanta.ApiCallBuilder.postponedCalls[i];
                if (call.getUrl() === selfUrl) {
                    return;
                }
            }

            lanta.ApiCallBuilder.postponedCalls.push(self);
            return;
        }

        if (!useCache) {
            self.query({ "_nocache": new Date().getDate() });
        }

        var xmlHttp = new XMLHttpRequest();
        xmlHttp.onreadystatechange = function () {
            if (xmlHttp.readyState !== 4) {
                return;
            }

            if (xmlHttp.status !== 200) {
                errorHandler(xmlHttp.status + ": " + xmlHttp.responseText);
                return;
            }

            var parsedResponse = xmlHttp.responseText;

            if (!noJson) {
                try {
                    parsedResponse = JSON.parse(parsedResponse);
                } catch (e) {
                }
            }

            responseHandler(parsedResponse);
            lt.notify();
        };

        try {

            if (!!queryString) {
                url = url + "?" + queryString;
            }

            xmlHttp.open(method, url, true);
            xmlHttp.send(bodyJson);
        } catch (error) {
            console.error(error);
            errorHandler(error.message);
        }
    }

    this.query = function (qry) {
        if (!qry) {
            return self;
        }

        for (var key in qry) {
            if (qry.hasOwnProperty(key)) {

                var value = qry[key];
                if (value === null || value === undefined) {
                    continue;
                }

                if (queryString.length > 0) {
                    queryString = queryString + "&";
                }
                queryString = queryString + escape(key) + "=" + escape(value.toString());
            }
        }

        return self;
    };

    this.body = function (obj) {
        bodyJson = JSON.stringify(obj);
        return self;
    };

    this.onerror = function (handler) {
        errorHandler = handler || errorHandler;
        return self;
    };

    this.get = function (handler) {
        call("GET", handler);
    };

    this.post = function (handler) {
        call("POST", handler);
    };

    this.useCache = function () {
        useCache = true;
        return self;
    };

    this.noJson = function () {
        noJson = true;
        return self;
    };

    this.ignoreDisabledApi = function () {
        throughDisabled = true;
        return self;
    };

    this.getUrl = function () {
        var result = url;
        if (!!queryString) {
            result = result + "?" + queryString;
        }

        return result;
    };
};

lanta.ApiCallBuilder.isEnabled = lanta.ApiCallBuilder.isEnabled || true;
lanta.ApiCallBuilder.postponedCalls = lanta.ApiCallBuilder.postponedCalls || [];

var lt = lt || {};
lt.api = lt.api || function (url) {
    return new lanta.ApiCallBuilder(url);
};

lt.api.disable = lt.api.disable || function () {
    lanta.ApiCallBuilder.isEnabled = false;
};

lt.api.enable = lt.api.enable = function () {

    lanta.ApiCallBuilder.isEnabled = true;

    while (lanta.ApiCallBuilder.postponedCalls.length > 0) {
        var postponedCall = lanta.ApiCallBuilder.postponedCalls.pop();
        postponedCall.call();
    }
};