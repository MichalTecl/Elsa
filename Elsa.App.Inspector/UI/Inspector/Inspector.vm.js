var app = app || {};
app.Inspector = app.Inspector || {};
app.Inspector.VM = app.Inspector.VM ||
function() {
    var self = this;

    self.totalIssues = 0;

    self.summary = [];

    self.isLoading = true;
    self.isWorried = false;
    self.isMad = false;
    self.isHappy = false;

    var autoRefresh = false;

    self.crossUserIssues = [];
    self.selectedUserId = null;

    var ensureSummaryNodeState = function(sum) {
        sum.isExpanded = !!sum.isExpanded;
        sum.issues = sum.issues || [];
        sum.canLoadMore = (sum.canLoadMore === undefined) ? true : sum.canLoadMore;
        sum.nextPageIndex = (sum.nextPageIndex === undefined) ? 0 : sum.nextPageIndex;
        sum.assignedUsers = sum.assignedUsers || [];
        sum.showAssignmentsPopup = !!sum.showAssignmentsPopup;
        sum.isZeroAssignments = !!sum.ShowAssignments && ((sum.AssignedUsersCount || 0) === 0);
    };

    var closeAssignmentsPopups = function(exceptTypeId) {
        self.summary.forEach(function(sum) {
            if (sum.TypeId !== exceptTypeId) {
                sum.showAssignmentsPopup = false;
            }
        });
    };

    var applyAssignments = function(typeModel, assignments) {
        typeModel.assignedUsers = assignments || [];

        typeModel.assignedUsers.forEach(function(user) {
            user.inspectionTypeId = typeModel.TypeId;
        });

        typeModel.AssignedUsersCount = typeModel.assignedUsers.filter(function(user) {
            return user.Assigned;
        }).length;

        typeModel.isZeroAssignments = typeModel.AssignedUsersCount === 0;
        typeModel.showAssignmentsPopup = true;

        lt.notify();
    };

    var getType = function(typeId) {

        for (var i = 0; i < self.summary.length; i++) {
            var s = self.summary[i];

            if (s.TypeId === typeId) {
                return s;
            }
        }

        return null;
    };

    this.issueActionCallback = function(issueId, actionText) {
        lt.api("/inspector/reevalIssue").query({ "issueId": issueId, "actionText": actionText }).get(receiveIssues);
    };

    var receiveIssues = function (issues) {
    
        var node = getType(issues.InspectionTypeId);
        if (!node) {
            console.error("Unexpected issueType");
            return;
        }
        
        for (var issue of issues.Issues) {

            var replaceIndex = -1;

            for (var ii = 0; ii < node.issues.length; ii++) {
                if (node.issues[ii].IssueId === issue.IssueId) {
                    replaceIndex = ii;
                    break;
                }
            }

            if (replaceIndex < 0) {
                node.issues.push(issue);
            }
            
            issue.hasActions = issue.ActionControls.length > 0;
            issue.isExpanded = false;
            
            issue.actions = [];
            for (var ctrl of issue.ActionControls) {
            
                const actionModel = {
                    "actionText": ctrl.ActionText,
                    "issueId": issue.IssueId,
                    "control": ctrl.Control,
                    "data": issue.Data
                };

                actionModel.callback = function() {
                    self.issueActionCallback(actionModel.issueId, actionModel.actionText);
                };

                issue.actions.push(actionModel);
            }

            if (replaceIndex > -1) {
                node.issues[replaceIndex] = issue;
            }
        }

        if (issues.NextPageIndex !== null) {
            node.canLoadMore = issues.NextPageIndex > 0;
            node.nextPageIndex = issues.NextPageIndex;
        }
    };

    var receiveSummary = function(summary) {

        self.totalIssues = 0;

        for (var sum of summary) {

            self.totalIssues += sum.IssuesCount;

            var found = false;
            for (var i = 0; i < self.summary.length; i++) {
                var local = self.summary[i];
                if (local.TypeId === sum.TypeId) {
                    local.IssuesCount = sum.IssuesCount;
                    local.ShowAssignments = sum.ShowAssignments;
                    local.AssignedUsersCount = sum.AssignedUsersCount || 0;
                    local.isZeroAssignments = !!local.ShowAssignments && (local.AssignedUsersCount === 0);
                    found = true;
                    break;
                }
            }

            if (found) {
                continue;
            }

            self.summary.push(sum);
            ensureSummaryNodeState(sum);
        }

        self.isLoading = false;
        self.isHappy = false;
        self.isMad = false;
        self.isWorried = false;

        self.isHappy = self.totalIssues === 0;
        self.isMad = self.totalIssues > 0;

        if (autoRefresh) {
            setTimeout(loadSummary, 60 * 1000);
        }
    };

    var receiveCrossUserIssues = function (issues) {
        self.crossUserIssues = issues;  

        if (self.selectedUserId === null && issues.length > 0) {
            self.selectedUserId = issues[0].UserId;
        }

        self.crossUserIssues.forEach(function (u) {
            u.activeTab = (u.UserId === self.selectedUserId) ? 1 : 0;
        });

    };

    var loadSummary = function () {
        closeAssignmentsPopups(null);
        lt.api("/inspector/getSummary").query({ "userId": self.selectedUserId }).get(receiveSummary);

        lt.api("/inspector/getUsersIssuesCounts").get(receiveCrossUserIssues);
    };

    self.openAssignmentsDialog = (model) => {
        if (!model.ShowAssignments) {
            return;
        }

        if (model.showAssignmentsPopup) {
            model.showAssignmentsPopup = false;
            lt.notify();
            return;
        }

        closeAssignmentsPopups(model.TypeId);

        lt.api("/inspector/GetAssignments").query({ "inspectionTypeId": model.TypeId }).get((assignments) => {
            applyAssignments(model, assignments);
        });
    };

    self.toggleAssignment = function(inspectionTypeId, userId, assign) {
        var typeModel = getType(inspectionTypeId);

        if (!typeModel) {
            return;
        }

        lt.api("/inspector/ChangeAssignment")
            .query({ "inspectionTypeId": inspectionTypeId, "userId": userId, "assign": assign })
            .get(function(assignments) {
                applyAssignments(typeModel, assignments);
            });
    };
    
    self.collapseType = function(typeId) {
        getType(typeId).isExpanded = false;
        lt.notify();
    };

    self.expandType = function (typeId) {
        var node = getType(typeId);
        node.isExpanded = true;
        lt.notify();

        if (node.issues.length > 0) {
            return;
        }

        setTimeout(function() {
            lt.api("/inspector/getIssues").query({ "inspectionTypeId": node.TypeId, "pageIndex": 0 })
                .get(receiveIssues);

        }, 0);
    };

    this.loadIssues = function(typeId) {

        var node = getType(typeId);
        lt.api("/inspector/getIssues").query({ "inspectionTypeId": node.TypeId, "pageIndex": node.nextPageIndex })
            .get(receiveIssues);
    };

    this.init = function(autorefresh) {
        autoRefresh = autorefresh;
        loadSummary();
    };

    self.setActiveUser = function (userId) {
        if (userId === self.selectedUserId)
            return; 

        closeAssignmentsPopups(null);
        self.selectedUserId = userId;
        receiveCrossUserIssues(self.crossUserIssues);
        self.summary = [];
        lt.notify();
        loadSummary();
    };

    document.addEventListener("click", function(event) {
        if (event.target.closest(".inspAssignmentsContainer")) {
            return;
        }

        var hadOpenPopup = false;

        self.summary.forEach(function(sum) {
            if (sum.showAssignmentsPopup) {
                sum.showAssignmentsPopup = false;
                hadOpenPopup = true;
            }
        });

        if (hadOpenPopup) {
            lt.notify();
        }
    });
};

app.Inspector.vm = app.Inspector.vm || new app.Inspector.VM();
