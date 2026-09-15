var app = app || {};
app.bulletinWidget = app.bulletinWidget || (function () {
    var requestId = 0;

    function refresh() {
        var link = document.querySelector("#repWidget .bulletinLink");
        if (!link) return;

        var session = app.user.vm.currentSession;
        var currentRequest = ++requestId;
        if (!session || !session.UserRights || session.UserRights.indexOf("ViewBulletin") < 0) {
            link.classList.remove("bulletinUnread");
            return;
        }

        // POST prevents browser/proxy caching of this user-specific, read-only status.
        lt.api("/reporting/getBulletinStatus").post(function (status) {
            if (currentRequest !== requestId || app.user.vm.currentSession !== session ||
                !status || typeof status.IsUnread !== "boolean") return;

            var currentLink = document.querySelector("#repWidget .bulletinLink");
            if (!currentLink) return;
            currentLink.classList.toggle("bulletinUnread", status.IsUnread);
            var label = status.IsUnread
                ? "Nový nepřečtený Bulletin"
                : (status.HasBulletin ? "Otevřít nejnovější Bulletin" : "Bulletin zatím není k dispozici");
            currentLink.title = label;
            currentLink.setAttribute("aria-label", label + " – otevřít v nové záložce");
        });
    }

    app.user.vm.subscribeUserChange(refresh);
    window.addEventListener("focus", refresh);
    document.addEventListener("visibilitychange", function () {
        if (!document.hidden) refresh();
    });
    // Also update if the new tab was opened in the background.
    document.addEventListener("click", function (event) {
        if (event.target.closest("#repWidget .bulletinLink")) {
            window.setTimeout(refresh, 1500);
            window.setTimeout(refresh, 5000);
        }
    });
    window.setInterval(function () {
        if (!document.hidden) refresh();
    }, 60000);

    return { refresh: refresh };
})();
app.bulletinWidget.refresh();