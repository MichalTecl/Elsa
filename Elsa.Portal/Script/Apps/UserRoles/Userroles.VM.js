var app = app || {};
app.userRoles = app.userRoles || {};
app.userRoles.VM = app.userRoles.VM || function() {
    var self = this;
    var selectedRoleId = -1;

    self.ChildRoles = [];


    var visitRoles = function(roles, visitor) {
        for (var i = 0; i < roles.length; i++) {
            var role = roles[i];
            visitor(role);

            visitRoles(role.ChildRoles, visitor);
        }
    };

    self.selectRole = function (roleId) {

        selectedRoleId = roleId;

        visitRoles(self.ChildRoles, function (role) {
            role.selected = role.Id === selectedRoleId;
            role.canDelete = (!role.ChildRoles) || role.ChildRoles.length === 0;
        });

        lt.notify();
    };
    
    var receiveRoleMap = function (map) {
        self.ChildRoles = map;
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
