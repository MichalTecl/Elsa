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
                    found = true;
                    break;
                }
            }

            if (found) {
                continue;
            }

            self.summary.push(sum);
            sum.isExpanded = false;
            sum.issues = [];
            sum.canLoadMore = true;
            sum.nextPageIndex = 0;
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

    var loadSummary = function() {
        lt.api("/inspector/getSummary").get(receiveSummary);
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
};

app.Inspector.vm = app.Inspector.vm || new app.Inspector.VM();