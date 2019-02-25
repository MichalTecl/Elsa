var app = app || {};
app.contextMenu = app.contextMenu || {};
app.contextMenu.shownMenus = app.contextMenu.shownMenus || [];

var setupContextMenu = function () {
    
    if ((!document) || (!document.body)) {
        setTimeout(setupContextMenu, 100);
        return;
    }

    if (document.body.showContextMenu) {
        return;
    }

    var closer = function () {

        if (window.event.isMenuOpen) {
            return;
        }

        while (app.contextMenu.shownMenus.length > 0) {
            var mnu = app.contextMenu.shownMenus.pop();
            mnu.style.display = 'none';
        }

        window.removeEventListener("click", closer);
    };

    document.body.showContextMenu = function(menu, caller) {
        closer();
        menu.style.display = 'block';
        app.contextMenu.shownMenus.push(menu);
        window.addEventListener("click", closer);

        window.event.isMenuOpen = true;

        var viewportOffset = caller.getBoundingClientRect();

        menu.style.left = viewportOffset.left + 'px';
        menu.style.top = viewportOffset.top + 'px';
    };

};
setupContextMenu();