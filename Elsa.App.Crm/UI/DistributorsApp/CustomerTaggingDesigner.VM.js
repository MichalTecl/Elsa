app = app || {};
app.CustomerTaggingDesigner = app.CustomerTaggingDesigner || {
    "VM": function () {

        const self = this;

        self.binder = 1;

        self.groups = [];
        self.groupsFilter = null;

        self.activeGroup = null;
        self.hasActiveGroup = false;

        self.openGroup = (id) => {
            self.activeGroup = self.groups.find(g => g.Id === id);
            self.hasActiveGroup = !!self.activeGroup;
        };

        self.closeGroup = () => {
            self.activeGroup = null;
            self.hasActiveGroup = !!self.activeGroup;
        }

        self.filterGroups = (filter) => {
            self.groupsFilter = filter;
            updateGroupsView();
        };

        self.createGroup = (name) => call("SaveGroup").query({ "name": name }).post(receiveGroups);
        self.getGroups = () => call("GetGroups").get(receiveGroups);

        const receiveGroups = (groups) => {
            self.closeGroup();
            self.groups = groups;

            self.groups.forEach(g => g.searchTags = g.SearchTag.split('|').slice(1).map(t => { return { "tag": t } }));

            updateGroupsView();
        };
        
        const updateGroupsView = () => {
            const matcher = new TextMatcher(self.groupsFilter);
            self.groups.forEach(g => g.isHidden = !matcher.match(g.SearchTag, true));
        };

        const call = (action) => {
            let rq = lt.api("/CustomerTagDesigner/" + action);

            if (!!self.activeGroup)
                rq = rq.query({ "groupId": self.activeGroup.Id });

            return rq;
        };

        self.getGroups();
    }
};

app.CustomerTaggingDesigner.vm = app.CustomerTaggingDesigner.vm || new app.CustomerTaggingDesigner.VM();