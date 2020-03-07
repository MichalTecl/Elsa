var app = app || {};
app.user = app.user || {};
app.user.ViewModel = app.user.ViewModel || function() {

    var self = this;
    var callbacks = [];

    lt.api.disable();
    this.currentSession = null;

    var onUserChanged = function (session) {

        if (!document.body) {
            setTimeout(onUserChanged, 100);
            return;
        }

        self.currentSession = session;

        if ((!!session) && (!!session.User)) {
            lt.api.enable();
        } else {
            lt.api.disable();
        }

        for (var i = 0; i < callbacks.length; i++) {
            var callback = callbacks[i];
            callback(session);
        }
    };
    
    this.login = function(user, pwd) {
        
        lt.api("/user/login").query({ "user": user, "password": pwd }).ignoreDisabledApi().get(function (session) {
            onUserChanged(session);
        });

    };

    this.logout = function() {
        lt.api("/user/logout").get(function() {
            onUserChanged(null);
            window.location.reload(false);
        });
    };
    
    this.subscribeUserChange = function(callback) {
        callbacks.push(callback);
    };

    this.changePassword = function(oldP, newP) {
        lt.api("/user/changePassword").query({ oldPassword:oldP, newPassword:newP }).get(function(session) {
            onUserChanged(session);
            window.location.reload(false);
        });
    };

    lt.api("/user/getCurrentSession").ignoreDisabledApi().get(function (session) {
        onUserChanged(session);
    });

    this.subscribeUserChange(function(session) {
        var restr = document.getElementById("restrstyle");
        if (!restr) {
            restr = document.createElement("style");
            document.body.appendChild(restr);
        }
        
        var stBuilder = [];
        stBuilder.push("[class*='restricted-']{ display:none !important; }\n");

        try {
            if (session && session.UserRights) {
                for (var i = 0; i < session.UserRights.length; i++) {
                    stBuilder.push(".restricted-");
                    stBuilder.push(session.UserRights[i]);
                    stBuilder.push("{ display:inherit !important;}\n");
                }
            }
        } catch (e) {
            console.error(e);
        }

        restr.innerHTML = stBuilder.join("");
    });
};

app.user.vm = app.user.vm || new app.user.ViewModel();

