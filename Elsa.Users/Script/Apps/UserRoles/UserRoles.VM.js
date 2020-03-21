var app = app || {};
app.userRoles = app.userRoles || {};
app.userRoles.VM = app.userRoles.VM || function() {
    var self = this;
    var selectedRoleId = -1;
    var allUsersCallbacks = [];
    var allUserNames = null;

    self.userRoles = [];
    self.roleRights = [];
    self.roleUsers = [];

    self.roleRightsPanelTitle = "Není vybrána žádná role";
    self.roleUsersPanelTitle = "Není vybrána žádná role";
    self.roleSelected = false;

    var visitRoles = function(roles, visitor) {
        for (var i = 0; i < roles.length; i++) {
            var role = roles[i];
            visitor(role);
            visitRoles(role.ChildRoles, visitor);
        }
    };

    var receiveRoleRights = function (rights) {

        var setCanAssign = function(rightsCollection, parentRightAssigned) {

            for (var i = 0; i < rightsCollection.length; i++) {
                var r = rightsCollection[i];
                r.canAssign = parentRightAssigned;
                setCanAssign(r.ChildRights, r.Assigned);
            }
        };

        setCanAssign(rights, true);

        self.roleRights = rights;
    };

    self.selectRole = function (roleId) {

        self.roleRights = [];

        var canSelect = false;

        self.roleRightsPanelTitle = "Není vybrána žádná role";
        self.roleUsersPanelTitle = "Není vybrána žádná role";
        self.roleSelected = false;

        visitRoles(self.userRoles, function(role) {
            if (role.Id === roleId && role.CanEdit) {
                canSelect = true;
                self.roleRightsPanelTitle = "Oprávnění přiřazená k roli " + role.Name;
                self.roleUsersPanelTitle = "Uživatelé v roli " + role.Name;
                self.roleSelected = true;
            }

            role.canDelete = (!role.ChildRoles) || role.ChildRoles.length === 0;
        });

        if (!canSelect) {
            return;
        }
        
        selectedRoleId = roleId;

        visitRoles(self.userRoles, function (role) {
            role.selected = role.Id === selectedRoleId;
        });

        self.roleUsers = [];

        lt.notify();

        if (selectedRoleId > -1) {
            lt.api("/userroles/getrolerightseditor").query({ "roleId": selectedRoleId }).get(receiveRoleRights);
            lt.api("/userroles/getrolemembers").query({ "roleId": selectedRoleId }).get(function(u) {
                self.roleUsers = u;
            });

        }
    };

    this.changeRoleRightAssignment = function (symbol, checked) {
        //self.roleRights = [];
        lt.notify();
        lt.api("/userroles/changeRolerightAssignment")
            .query({ "roleId": selectedRoleId, "rightSymbol": symbol, "assign": checked }).get(receiveRoleRights);
    };
    
    var receiveRoleMap = function (map) {
        self.userRoles = map;
        self.selectRole(selectedRoleId);
    };

    self.addChildRole = function (parentId, newName) {
        lt.api("/UserRoles/AddChildRole").query({ "parentId": parentId, "name": newName }).get(receiveRoleMap);
    };

    self.renameRole = function(id, newName) {
        lt.api("/UserRoles/RenameRole").query({ "roleId": id, "newName": newName }).get(receiveRoleMap);
    };

    self.deleteRole = function(id) {
        lt.api("/UserRoles/DeleteRole").query({ "roleId": id }).get(receiveRoleMap);
    };

    self.searchUsers = function (query, callback) {
        
        if (allUserNames) {
            callback(allUserNames);
            return;
        }

        allUsersCallbacks.push(callback);
    };

    var receiveUserNames = function(userNames) {
        allUserNames = userNames;

        while (allUsersCallbacks.length > 0) {
            var cbk = allUsersCallbacks.pop();
            cbk(allUserNames);
        }
    };

    self.validateUserName = function(userName) {

        for (var i = 0; i < allUserNames.length; i++) {
            if (allUserNames[i] === userName) {
                return true;
            }
        }

        return false;
    };

    self.addUserToRole = function(userName, callback) {
        if (selectedRoleId === -1) {
            return;
        }

        lt.api("/userroles/assignUser").query({ "roleId": selectedRoleId, "userName": userName })
            .get(function(usersInRole) {
                self.roleUsers = usersInRole;
                callback();
            });
    };

    self.unassignUserFromRole = function(userId) {
        if (selectedRoleId === -1) {
            return;
        }

        lt.api("/userroles/unassignUser").query({ "roleId": selectedRoleId, "userId": userId }).get(
            function(usersInRole) {
                self.roleUsers = usersInRole;
            });
    };

    var loadRoles = function() {
        lt.api("/userroles/getroles").get(receiveRoleMap);
        lt.api("/user/getAllUserNamesExceptMe").get(receiveUserNames);
    };

    
    setTimeout(loadRoles, 100);
};

app.userRoles.vm = app.userRoles.vm || new app.userRoles.VM();
