var app = app || {};
app.MailSummariserPromptEditor = app.MailSummariserPromptEditor || {};

app.MailSummariserPromptEditor.VM = app.MailSummariserPromptEditor.VM || function () {
    var self = this;

    self.prompts = [];
    self.editedPrompt = null;
    self.loading = false;

    const receivePrompts = function (prompts) {
        self.prompts = prompts || [];

        if (!!self.editedPrompt) {
            const selected = self.prompts.find(p => p.PromptId === self.editedPrompt.PromptId);
            self.editedPrompt = selected || null;
        }

        self.prompts.forEach(function (p) {
            p.IsSelected = !!self.editedPrompt && self.editedPrompt.PromptId === p.PromptId;
        });

        lt.notify();
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
        self.editedPrompt = self.prompts.find(p => p.PromptId === promptId) || null;
        self.prompts.forEach(function (p) {
            p.IsSelected = !!self.editedPrompt && self.editedPrompt.PromptId === p.PromptId;
        });
        lt.notify();
    };

    self.createPrompt = function () {
        lt.api("/MailSumPromptEditor/CreatePrompt")
            .post(function (prompts) {
                receivePrompts(prompts);
                self.editedPrompt = (prompts || [])[0] || null;
                self.prompts.forEach(function (p) {
                    p.IsSelected = !!self.editedPrompt && self.editedPrompt.PromptId === p.PromptId;
                });
                lt.notify();
            });
    };

    self.duplicatePrompt = function (promptId) {
        const original = self.prompts.find(p => p.PromptId === promptId);
        if (!original)
            return;

        lt.api("/MailSumPromptEditor/EditPrompt")
            .query({ promptText: original.Prompt })
            .post(function (prompts) {
                receivePrompts(prompts);
                self.editedPrompt = (prompts || [])[0] || null;
                self.prompts.forEach(function (p) {
                    p.IsSelected = !!self.editedPrompt && self.editedPrompt.PromptId === p.PromptId;
                });
                lt.notify();
            });
    };

    self.deletePrompt = function (promptId) {
        const original = self.prompts.find(p => p.PromptId === promptId);
        if (!original || !original.CanDelete)
            return;

        if (!confirm("Opravdu chceš smazat tento prompt?"))
            return;

        lt.api("/MailSumPromptEditor/DeletePrompt")
            .query({ promptId: promptId })
            .post(function (prompts) {
                if (!!self.editedPrompt && self.editedPrompt.PromptId === promptId)
                    self.editedPrompt = null;

                receivePrompts(prompts);
            });
    };

    self.reloadPrompts();
};

app.MailSummariserPromptEditor.vm = app.MailSummariserPromptEditor.vm || new app.MailSummariserPromptEditor.VM();
