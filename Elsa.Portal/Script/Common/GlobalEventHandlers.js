var app = app || {};
app.globalEventHandlers = app.globalEventHandlers || {};
app.globalEventHandlers.init = app.globalEventHandlers.init || function() {
    
    if ((!document) || (!document.body)) {
        setTimeout(app.globalEventHandlers.init, 100);
        return;
    }

    document.body.enterTo = document.body.enterTo || function(target) {
        if (window.event.key === "Enter") {
            target.click();
        }
    };
};

app.globalEventHandlers.init();