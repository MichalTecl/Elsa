var lanta = lanta || {};
lanta.ApiCallBuilder = lanta.ApiCallBuilder || function (url) {

    var self = this;
    var queryString = "";
    var bodyJson = null;
    var useCache = false;
    var noJson = false;
    var throughDisabled = false;
    var httpMethod = "GET";
    var silent = false;
    var formData = null;

    var responseHandler = function (resp) {
        console.log("No response handler. Request=" + url + " response:" + resp);
        lt.notify();
    };

    var errorHandler = function (err) { lanta.Extensions.defaultErrorHandler(new Error(err)); };

    var call = function(method, handler) {

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
            self.query({ "_nocache": new Date().getTime() });
        } else {
            self.query({ "_build": (window["__release"] || "?") });
        }

        var xmlHttp = new XMLHttpRequest();

        xmlHttp.onreadystatechange = function() {
            if (xmlHttp.readyState !== 4) {
                return;
            }

            if (xmlHttp.status !== 200) {
                if (!silent) {
                    lt.api.usageManager.notifyOperationEnd();
                }

                if (xmlHttp.status === 0) {
                    errorHandler("Nepodařilo se spojit se serverem, opakujte akci později...");
                    return;
                }

                errorHandler(xmlHttp.status + ": " + xmlHttp.responseText);
                return;
            }

            var parsedResponse = xmlHttp.responseText;

            if (parsedResponse.indexOf("ExecutionError") > -1) {
                try {
                    var errorObj = JSON.parse(parsedResponse);

                    if (errorObj.ExecutionError && errorObj.StatusCode > 499) {
                        try {
                            errorHandler(errorObj.StatusCode + ": " + errorObj.ExecutionError);
                            return;
                        } finally {
                            if (!silent) {
                                lt.api.usageManager.notifyOperationEnd();
                            }
                        }
                    }

                } catch (e) {
                    try {
                        console.error("Parsing of error response failed:" + e.toString());
                        return;
                    } finally {
                        if (!silent) {
                            lt.api.usageManager.notifyOperationEnd();
                        }
                    }
                }
            }

            if (!noJson) {
                try {
                    parsedResponse = JSON.parse(parsedResponse);
                } catch (e) {
                    console.warn("JSON expected - use .nojson modifier in call builder (" + url + ")");
                }
            }

            try {
                responseHandler(parsedResponse);
                lt.notify();
            } finally {
                if (!silent) {
                    lt.api.usageManager.notifyOperationEnd();
                }
            }
        };

        try {

            if (!!queryString) {
                url = url + "?" + queryString;
            }

            xmlHttp.open(httpMethod, url, true);
            
            if (!silent) {
                lt.api.usageManager.notifyOperationStart();
            }

            if (formData) {
                xmlHttp.send(formData);
            } else {
                xmlHttp.setRequestHeader('Content-type', '*/*; charset=utf-8');
                xmlHttp.send(bodyJson);
            }
        } catch (error) {
            if (!silent) {
                lt.api.usageManager.notifyOperationEnd();
            }
            console.error(error);
            errorHandler(error.message);
        }
    };

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
                queryString = queryString + encodeURIComponent(key) + "=" + encodeURIComponent(value.toString());
            }
        }

        return self;
    };

    this.body = function (obj) {
        bodyJson = JSON.stringify(obj);
        return self;
    };

    this.formData = function(name, value) {

        formData = formData || new FormData();
        formData.append(name, value);
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

    this.call = function() {
        call();
    };

    this.silent = function() {
        silent = true;
        return self;
    };

    this.downloadFile = function() {
        var lnk = document.createElement("a");
        lnk.setAttribute("hidden", true);
        lnk.setAttribute("download", true);
        lnk.setAttribute("href", self.getUrl());

        document.body.appendChild(lnk);

        lnk.click();
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


lt.api.UsageManager = lt.api.UsageManager || function() {

    var self = this;

    var pendingOpsCounter = 0; 
    var handlers = [];

    var notifyHandlers = function(busy) {
      for (var i = 0; i < handlers.length; i++) {
          var h = handlers[i];
          h(busy);
      }  
    };

    self.notifyOperationStart = function() {
        pendingOpsCounter++;

        if (pendingOpsCounter === 1) {
            notifyHandlers(true);
        }
    };

    self.notifyOperationEnd = function() {
        pendingOpsCounter--;
        if (pendingOpsCounter === 0) {
            notifyHandlers(false);
        }

        if (pendingOpsCounter < 0) {
            pendingOpsCounter = 0;
        }
    };

    self.subscribeBusyHandler = function(handler) {
        handlers.push(handler);
        handler(pendingOpsCounter > 0);
    };
};

lt.api.usageManager = lt.api.usageManager || new lt.api.UsageManager();

