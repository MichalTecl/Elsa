var app = app || {};
app.customers = app.customers || {};
app.customers.ViewModel = app.customers.ViewModel || function() {
    var self = this;
    var cache = {};
    var callbacks = {};

    self.customerDialogPosition = {x:0,y:0};
    self.selectedCustomer = null;

    var receiveCustomerData = function (data) {

        data.Email = data.Email.toLowerCase();

        cache[data.Email] = data;

        data.hasOmittedOrders = (!!data.OmittedOrders) && (data.OmittedOrders > 0);

        var callbackList = callbacks[data.Email];
        if (!callbackList) {
            delete callbacks[data.Email];
            return;
        }

        while (callbackList.length > 0) {
            var callback = callbackList.shift();
            callback(data);
        }

        delete callbacks[data.Email];
    };

    self.getCustomer = function (email, callback) {
        email = email.toLowerCase();
        var cached = cache[email];
        if (!!cached) {
            callback(cached);
            return;
        }
        
        var callbackList = callbacks[email];
        if (!callbackList) {
            alreadyRequested = false;
            callbackList = [];
            callbacks[email] = callbackList;
        };
        callbackList.push(callback);
        
        //if (!alreadyRequested) {
        //    lt.api("/customers/getCustomer")
        //        .query({ "email": email })
        //        .silent()
        //        .get(function(result) {
        //            receiveCustomerData(result);
        //        });
        //}
    };
    
    self.selectCustomer = function (email, dialogX, dialogY) {
        console.log("Requested selectCustomer('" + email + ')');
        self.customerDialogPosition = { x: dialogX, y: dialogY };
        self.getCustomer(email, function(data) {
            self.selectedCustomer = data;
            console.log("SelectedCustomer = " + data.Email);
            lt.notify();
        });
    };

    self.cancelCustomerSelection = function() {
        self.selectedCustomer = null;
        lt.notify();
    };

    var loaderTick = function() {

        var request = [];
        for (var mail in callbacks) {
            if (callbacks.hasOwnProperty(mail)) {
                request.push(mail);
            }
        }

        if (request.length === 0) {
            setTimeout(loaderTick, 500);
            return;
        }

        lt.api("/customers/getCustomers").silent().body(request).post(function(customers) {
            
            try {

                for (var i = 0; i < customers.length; i++) {
                    var cust = customers[i];
                    receiveCustomerData(cust);
                }

            } finally {
                setTimeout(loaderTick, 0);
            }
        });
    };

    setTimeout(loaderTick, 500);
};


app.customers.vm = app.customers.vm || new app.customers.ViewModel();
lt.notify();