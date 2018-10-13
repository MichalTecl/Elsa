var app = app || {};
app.customers = app.customers || {};
app.customers.ViewModel = app.customers.ViewModel || function() {
    var self = this;
    const cacheLifetime = 1000 * 60 * 2; //2 minutes
    var cache = {};
    var selectedCustomerEmail = null;

    self.selectedCustomer = null;

    var receiveCustomerData = function(data) {
        data.entryLifetime = new Date().getTime() + cacheLifetime;
        cache[data.Email] = data;
    };

    var getCustomerData = function(email) {
        
    };

    self.getCustomer = function(email, callback) {
        var cached = cache[email];
        if ((!!cached) && ((new Date().getTime() - cached.entryLifetime) < cacheLifetime)) {
            callback(cached);
            return;
        }

        lt.api("/customers/getCustomer").query({ "email": email }).silent().get(function(result) {

            receiveCustomerData(result);
            callback(result);
        });
    };

    self.selectCustomer = function(email) {
        self.getCustomer(email, function(data) {
            self.selectedCustomer = data;
        });
    };
};

app.customers.vm = app.customers.vm || new app.customers.ViewModel();
lt.notify();