var app = app || {};
app.MailSummariserPromptEditor = app.MailSummariserPromptEditor || {};

app.MailSummariserPromptEditor.VM = app.MailSummariserPromptEditor.VM || function () {
    var self = this;

    self.prompts = [];
    self.editedPrompt = null;
    self.editedPromptText = "";
    self.originalPromptText = "";
    self.loading = false;

    self.activateDialogOpen = false;
    self.activateRebuildSummaries = false;

    self.testSetOptions = [1, 5, 10, 20];
    self.testSetSize = 5;
    self.isTesting = false;
    self.testStopRequested = false;
    self.testConversationIds = [];
    self.testMeetings = [];
    self.testRequestedCount = 0;
    self.testLoadedCount = 0;
    self.testProgressPercent = 0;

    self.mailConversationDialogOpen = false;
    self.mailConversationDetail = [];
    self.mailConversationTitle = null;

    self.promptReadOnly = true;
    self.canSavePrompt = false;
    self.canDiscardPrompt = false;
    self.canActivatePrompt = false;
    self.canRunTests = false;
    self.detailOpen = false;
    self.listOnly = true;

    const formatPreviewDt = function (datetimeValue) {
        const now = new Date();
        const datetime = new Date(datetimeValue);

        const day = String(datetime.getDate()).padStart(2, '0');
        const month = String(datetime.getMonth() + 1).padStart(2, '0');
        const year = datetime.getFullYear();

        if (datetime < now) {
            return `${day}.${month}.${year}`;
        }

        const hours = String(datetime.getHours()).padStart(2, '0');
        const minutes = String(datetime.getMinutes()).padStart(2, '0');
        return `${day}.${month}.${year} ${hours}:${minutes}`;
    };

    const normalizeMeeting = function (meeting) {
        if (!meeting)
            return meeting;

        const normalized = Object.assign({}, meeting);
        normalized.previewDt = formatPreviewDt(normalized.StartDt);
        normalized.isOpen = false;
        normalized.isMailConversation = !!normalized.MailConversationId;
        normalized.isReadOnly = true;
        normalized.canOpenConversation = normalized.isMailConversation && !!(window.can && can.EmailConversationsFull);
        normalized.Actions = normalized.Actions || [];
        normalized.Participants = normalized.Participants || [];
        normalized.Actions.forEach(function (a) { a.meetingId = normalized.Id; });
        normalized.textDirty = false;

        return normalized;
    };

    const refreshPromptFlags = function () {
        const hasPrompt = !!self.editedPrompt;
        const canEditPrompt = hasPrompt && !self.editedPrompt.IsActive;
        const hasChanges = canEditPrompt && self.editedPromptText !== self.originalPromptText;

        self.detailOpen = hasPrompt;
        self.listOnly = !hasPrompt;
        self.promptReadOnly = !canEditPrompt || self.isTesting;
        self.canSavePrompt = hasChanges && !self.isTesting;
        self.canDiscardPrompt = hasChanges && !self.isTesting;
        self.canActivatePrompt = canEditPrompt && !self.isTesting;
        self.canRunTests = hasPrompt && !self.isTesting;
    };

    const syncSelection = function (promptId, syncText) {
        self.editedPrompt = self.prompts.find(function (p) { return p.PromptId === promptId; }) || null;

        self.prompts.forEach(function (p) {
            p.IsSelected = !!self.editedPrompt && self.editedPrompt.PromptId === p.PromptId;
        });

        if (!!self.editedPrompt && syncText !== false) {
            self.originalPromptText = self.editedPrompt.Prompt || "";
            self.editedPromptText = self.originalPromptText;
        }

        if (!self.editedPrompt) {
            self.originalPromptText = "";
            self.editedPromptText = "";
        }

        refreshPromptFlags();
        lt.notify();
    };

    const receivePrompts = function (prompts, preferredPromptId) {
        self.prompts = prompts || [];

        var selectedPromptId = preferredPromptId;
        if (!selectedPromptId && !!self.editedPrompt)
            selectedPromptId = self.editedPrompt.PromptId;

        syncSelection(selectedPromptId, true);
    };

    const ensureNoUnsavedChanges = function () {
        if (!self.canSavePrompt)
            return true;

        return window.confirm("Máš neuložené změny. Opravdu chceš pokračovat?");
    };

    const savePromptInternal = function (callback) {
        if (!self.editedPrompt)
            return;

        lt.api("/MailSumPromptEditor/EditPrompt")
            .body({ id: self.editedPrompt.PromptId, promptText: self.editedPromptText })
            .post(function (prompts) {
                receivePrompts(prompts, self.editedPrompt.PromptId);

                if (!!callback)
                    callback();
            });
    };

    const finishTesting = function () {
        self.isTesting = false;
        self.testStopRequested = false;
        refreshPromptFlags();
        lt.notify();
    };

    const continueTesting = function () {
        if (self.testStopRequested || self.testLoadedCount >= self.testConversationIds.length) {
            finishTesting();
            return;
        }

        const conversationId = self.testConversationIds[self.testLoadedCount];

        lt.api("/MailSumPromptEditor/DoTestSummary")
            .body({
                promptId: self.editedPrompt.PromptId,
                conversationId: conversationId,
                promptText: self.editedPromptText
            })
            .silent()
            .post(function (meeting) {
                const normalizedMeeting = normalizeMeeting(meeting);
                self.testMeetings.push(normalizedMeeting);
                self.testLoadedCount++;
                self.testProgressPercent = self.testRequestedCount < 1
                    ? 0
                    : Math.round((self.testLoadedCount / self.testRequestedCount) * 100);
                lt.notify();
                continueTesting();
            });
    };

    self.reloadPrompts = function () {
        self.loading = true;
        lt.notify();

        lt.api("/MailSumPromptEditor/GetPrompts")
            .get(function (prompts) {
                self.loading = false;
                receivePrompts(prompts);
            });
    };

    self.selectPrompt = function (promptId) {
        if (self.isTesting)
            return;

        const nextPrompt = self.prompts.find(function (p) { return p.PromptId === promptId; });
        if (!nextPrompt)
            return;

        if (!ensureNoUnsavedChanges())
            return;

        syncSelection(nextPrompt.PromptId, true);
    };

    self.closeDetail = function () {
        if (self.isTesting)
            return;

        if (!ensureNoUnsavedChanges())
            return;

        syncSelection(null, true);
    };

    self.updatePromptText = function (value) {
        self.editedPromptText = value || "";
        refreshPromptFlags();
        lt.notify();
    };

    self.discardPromptChanges = function () {
        if (!self.editedPrompt || self.promptReadOnly)
            return;

        self.editedPromptText = self.originalPromptText;
        refreshPromptFlags();
        lt.notify();
    };

    self.savePrompt = function () {
        if (!self.canSavePrompt)
            return;

        savePromptInternal();
    };

    self.createPrompt = function () {
        if (self.isTesting)
            return;

        lt.api("/MailSumPromptEditor/CreatePrompt")
            .post(function (prompts) {
                const selected = (prompts || [])[0];
                receivePrompts(prompts, !!selected ? selected.PromptId : null);
            });
    };

    self.duplicatePrompt = function (promptId) {
        if (self.isTesting)
            return;

        const original = self.prompts.find(function (p) { return p.PromptId === promptId; });
        if (!original)
            return;

        lt.api("/MailSumPromptEditor/EditPrompt")
            .body({ promptText: original.Prompt })
            .post(function (prompts) {
                const selected = (prompts || [])[0];
                receivePrompts(prompts, !!selected ? selected.PromptId : null);
            });
    };

    self.deletePrompt = function (promptId) {
        if (self.isTesting)
            return;

        const original = self.prompts.find(function (p) { return p.PromptId === promptId; });
        if (!original || !original.CanDelete)
            return;

        if (!window.confirm("Opravdu chceš smazat tento prompt?"))
            return;

        lt.api("/MailSumPromptEditor/DeletePrompt")
            .query({ promptId: promptId })
            .post(function (prompts) {
                if (!!self.editedPrompt && self.editedPrompt.PromptId === promptId)
                    self.editedPrompt = null;

                receivePrompts(prompts);
            });
    };

    self.openActivateDialog = function () {
        if (!self.canActivatePrompt)
            return;

        const openDialog = function () {
            self.activateRebuildSummaries = false;
            self.activateDialogOpen = true;
            lt.notify();
        };

        if (self.canSavePrompt) {
            savePromptInternal(openDialog);
            return;
        }

        openDialog();
    };

    self.closeActivateDialog = function () {
        self.activateDialogOpen = false;
        self.activateRebuildSummaries = false;
        lt.notify();
    };

    self.setActivateRebuild = function (value) {
        self.activateRebuildSummaries = !!value;
        lt.notify();
    };

    self.confirmActivatePrompt = function () {
        if (!self.editedPrompt)
            return;

        lt.api("/MailSumPromptEditor/ConfirmPrompt")
            .query({
                promptId: self.editedPrompt.PromptId,
                rebuildSummaries: self.activateRebuildSummaries
            })
            .post(function (prompts) {
                self.activateDialogOpen = false;
                self.activateRebuildSummaries = false;
                receivePrompts(prompts, self.editedPrompt.PromptId);
            });
    };

    self.startTestRun = function () {
        if (!self.canRunTests || !self.editedPrompt)
            return;

        self.isTesting = true;
        self.testStopRequested = false;
        self.testConversationIds = [];
        self.testMeetings = [];
        self.testRequestedCount = 0;
        self.testLoadedCount = 0;
        self.testProgressPercent = 0;
        refreshPromptFlags();
        lt.notify();

        lt.api("/MailSumPromptEditor/PrepareConversationTestSet")
            .query({ size: self.testSetSize })
            .post(function (conversationIds) {
                self.testConversationIds = conversationIds || [];
                self.testRequestedCount = self.testConversationIds.length;
                self.testLoadedCount = 0;
                self.testProgressPercent = 0;
                lt.notify();

                continueTesting();
            });
    };

    self.stopTestRun = function () {
        self.testStopRequested = true;
        lt.notify();
    };

    self.openMeetingDetail = function (id) {
        const nowOpen = self.testMeetings.find(function (m) { return m.isOpen; });

        if (!!nowOpen) {
            nowOpen.isOpen = false;

            if (nowOpen.Id === id) {
                lt.notify();
                return;
            }
        }

        self.testMeetings.forEach(function (m) { m.isOpen = m.Id === id; });
        lt.notify();
    };

    self.closeMailConversation = function () {
        self.mailConversationDialogOpen = false;
        self.mailConversationDetail = [];
        self.mailConversationTitle = null;
        lt.notify();
    };

    self.openMailConversation = function (meetingId) {
        const meeting = self.testMeetings.find(function (m) { return m.Id === meetingId; });

        if (!meeting || !meeting.MailConversationId || !meeting.canOpenConversation)
            return;

        lt.api("/MailSumPromptEditor/GetMailConversationDetail")
            .query({ conversationId: meeting.MailConversationId })
            .get(function (messages) {
                self.mailConversationTitle = meeting.Title || "E-mailová konverzace";
                self.mailConversationDetail = (messages || []).map(function (m) {
                    return Object.assign({}, m, {
                        Content: (m.Content || "").replace(/(\r?\n){3,}/g, "\n\n").trim()
                    });
                });
                self.mailConversationDialogOpen = true;
                lt.notify();
            });
    };

    self.stopEvent = function (e) {
        if (!!e && !!e.stopPropagation)
            e.stopPropagation();
    };

    self.meetingTextChange = function () { };
    self.saveMeetingText = function () { };
    self.setMeetingStatus = function () { };
    self.editMeeting = function () { };

    self.reloadPrompts();
};

app.MailSummariserPromptEditor.vm = app.MailSummariserPromptEditor.vm || new app.MailSummariserPromptEditor.VM();
