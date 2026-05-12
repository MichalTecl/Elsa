var app = app || {};
app.CrmMailboxAdministration = app.CrmMailboxAdministration || {};

app.CrmMailboxAdministration.VM = app.CrmMailboxAdministration.VM || function () {
    var self = this;

    self.sources = [];
    self.isLoading = false;
    self._newRowSequence = 0;

    var createSnapshot = function (source) {
        return JSON.stringify({
            Host: source.Host || "",
            Port: parseInt(source.Port, 10) || 0,
            UseSsl: !!source.UseSsl,
            Username: source.Username || "",
            IsEnabled: !!source.IsEnabled,
            password: source.password || ""
        });
    };

    var normalizeFolder = function (folder) {
        folder = folder || {};
        folder.IsEnabled = !!folder.IsEnabled;
        return folder;
    };

    var normalizeSource = function (source) {
        source = source || {};
        source.UseSsl = !!source.UseSsl;
        source.IsEnabled = !!source.IsEnabled;
        source.folders = (source.Folders || []).map(normalizeFolder);
        source.isExpanded = !!source.isExpanded;
        source.password = "";
        source.hasPassword = !!source.HasPassword;
        source.hasFolders = source.folders.length > 0;
        source.RowKey = source.RowKey || source.Id || ("new_" + (++self._newRowSequence));
        source._originalState = createSnapshot(source);
        source.isDirty = false;
        return source;
    };

    var refreshDirty = function (source) {
        source.isDirty = source._originalState !== createSnapshot(source);
    };

    var receiveSources = function (sources) {
        var expansionMap = {};
        self.sources.forEach(function (s) {
            expansionMap[s.RowKey] = !!s.isExpanded;
        });

        self.sources = (sources || []).map(function (source) {
            var normalized = normalizeSource(source);
            normalized.isExpanded = !!expansionMap[normalized.RowKey];
            return normalized;
        });

        lt.notify();
    };

    self.load = function () {
        self.isLoading = true;
        lt.notify();

        lt.api("/CrmMailboxAdministration/GetSources")
            .get(function (sources) {
                self.isLoading = false;
                receiveSources(sources);
            });
    };

    self.addSource = function () {
        self.sources.unshift(normalizeSource({
            Id: null,
            Host: "",
            Port: 993,
            UseSsl: true,
            Username: "",
            IsEnabled: false,
            HasPassword: false,
            Folders: [],
            RowKey: "new_" + (++self._newRowSequence)
        }));

        self.sources[0].isExpanded = true;
        lt.notify();
    };

    self.toggleExpanded = function (source) {
        source.isExpanded = !source.isExpanded;
        lt.notify();
    };

    self.updateSourceText = function (source, field, value) {
        source[field] = value;
        refreshDirty(source);
        lt.notify();
    };

    self.updateSourceBool = function (source, field, value) {
        source[field] = !!value;
        refreshDirty(source);
        lt.notify();
    };

    self.saveSource = function (source) {
        if (!source.isDirty)
            return;

        self.isLoading = true;
        lt.notify();

        lt.api("/CrmMailboxAdministration/SaveSource")
            .body({
                Id: source.Id,
                Host: source.Host,
                Port: parseInt(source.Port, 10) || 0,
                UseSsl: !!source.UseSsl,
                Username: source.Username,
                Password: source.password,
                IsEnabled: !!source.IsEnabled
            })
            .post(function (result) {
                self.isLoading = false;
                receiveSources(result.Sources);
                alert(result.ConnectionTestMessage);
            });
    };

    self.saveFolderEnabled = function (folder, isEnabled) {
        self.isLoading = true;
        lt.notify();

        lt.api("/CrmMailboxAdministration/SetFolderEnabled")
            .query({ folderId: folder.Id, isEnabled: !!isEnabled })
            .post(function (sources) {
                self.isLoading = false;
                receiveSources(sources);
                alert("Nastavení složky bylo uloženo.");
            });
    };

    self.load();
};

app.CrmMailboxAdministration.vm = app.CrmMailboxAdministration.vm || new app.CrmMailboxAdministration.VM();
