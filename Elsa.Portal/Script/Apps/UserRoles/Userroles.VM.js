var app = app || {};
app.userRoles = app.userRoles || {};
app.userRoles.VM = app.userRoles.VM || function() {
    var self = this;
    var selectedRoleId = -1;

    self.userRoles = [];
    self.roleRights = [];
    self.roleUsers = [];

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

        visitRoles(self.userRoles, function(role) {
            if (role.Id === roleId && role.CanEdit) {
                canSelect = true;
                return;
            }
        });

        if (!canSelect) {
            return;
        }

        selectedRoleId = roleId;

        visitRoles(self.userRoles, function (role) {
            role.selected = role.Id === selectedRoleId;
            role.canDelete = (!role.ChildRoles) || role.ChildRoles.length === 0;
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

    var loadRoles = function() {
        lt.api("/userroles/getroles").get(receiveRoleMap);
    };

    setTimeout(loadRoles, 100);
};

app.userRoles.vm = app.userRoles.vm || new app.userRoles.VM();
