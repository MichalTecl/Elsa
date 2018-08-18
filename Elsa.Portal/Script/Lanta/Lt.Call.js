var lanta = lanta || {};
lanta.ApiCallBuilder = lanta.ApiCallBuilder || function(url) {

    var self = this;
    var queryString = "";
    var bodyJson = null;
    var useCache = false;
    var noJson = false;

    var responseHandler = function (resp) {
    	console.log("No response handler. Request=" + url + " response:" + resp);
        lt.notify();
    };

    var errorHandler = function (err) { lanta.Extensions.defaultErrorHandler(new Error(err)); };

    var call = function (method, handler) {

        if (!useCache) {
            self.query({ "_nocache": new Date().getDate() });
        }

    	responseHandler = handler || responseHandler;

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
	
    this.query = function(qry) {
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

    this.body = function(obj) {
    	bodyJson = JSON.stringify(obj);
        return self;
    };

    this.onerror = function(handler) {
    	errorHandler = handler || errorHandler;
        return self;
    };

    this.get = function(handler) {
        call("GET", handler);
    };

    this.post = function(handler) {
        call("POST", handler);
    };

    this.useCache = function() {
        useCache = true;
        return self;
    };

    this.noJson = function() {
        noJson = true;
        return self;
    };
};

var lt = lt || {};
lt.api = lt.api || function(url) {
    return new lanta.ApiCallBuilder(url);
};