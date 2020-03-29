var app = app || {};
app.UserManager = app.UserManager || {};
app.UserManager.VM = app.UserManager.VM || function() {
    var self = this;

    self.users = [];

    var receiveUserList = function(users) {
        self.users = users;
    };

    var loadUsers = function() {
        lt.api("/user/getAllUsersExceptMe").get(receiveUserList);
    };

    setTimeout(loadUsers, 20);

    self.inviteUser = function(email) {
        lt.api("/user/InviteUser").query({ "email": email }).get(receiveUserList);
    };

    self.resetPassword = function(userId, callback) {
        lt.api("/user/resetPassword").query({ "userId": userId }).get(function(ul) {
                receiveUserList(ul);
                callback();
            }
        );
    };

    self.toggleAccountLock = function(userId) {
        var isLocking = null;

        for (var i = 0; i < self.users.length; i++) {
            var u = self.users[i];
            if (u.Id !== userId) {
                continue;
            }

            isLocking = !u.IsLocked;
            break;
        }

        if (isLocking === null) {
            log.error("didn't find the user record");
            return;
        }

        lt.api("/user/toggleAccountLock").query({ "userId": userId, "locked": isLocking }).get(receiveUserList);
    };
};

app.UserManager.vm = app.UserManager.vm || new app.UserManager.VM();