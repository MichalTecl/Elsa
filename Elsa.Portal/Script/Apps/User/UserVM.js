var app = app || {};
app.user = app.user || {};
app.user.ViewModel = app.user.ViewModel || function() {

    var self = this;
    var callbacks = [];

    this.currentSession = null;

    var onUserChanged = function (session) {
        
        self.currentSession = session;
        for (var i = 0; i < callbacks.length; i++) {
            var callback = callbacks[i];
            callback();
        }
    };
    
    this.login = function(user, pwd) {
        
        lt.api("/user/login").query({ "user": user, "password": pwd }).get(function (session) {
            onUserChanged(session);
        });

    };

    this.logout = function() {
        lt.api("/user/logout").get(function() {
            onUserChanged(null);
            window.location.reload(false);
        });
    }
    
    this.subscribeUserChange = function(callback) {
        callbacks.push(callback);
    };

    this.changePassword = function(oldP, newP) {
        lt.api("/user/changePassword").query({ oldPassword:oldP, newPassword:newP }).get(function(session) {
            onUserChanged(session);
            window.location.reload(false);
        });
    };

    lt.api("/user/getCurrentSession").get(function (session) {
        onUserChanged(session);
    });


};

app.user.vm = app.user.vm || new app.user.ViewModel();