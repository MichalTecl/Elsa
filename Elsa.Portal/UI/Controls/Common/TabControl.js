var app = app || {};
app.ui = app.ui || {};
app.ui.TabControl = app.ui.TabControl || function(holder) {

    var self = this;

    self.autoselect = true;

    var tabs = [];

    var headersContainer = document.createElement("div");
    headersContainer.setAttribute("class", "tabPanelHeadersContainer");
    holder.appendChild(headersContainer);

    var contentPanel = document.createElement("div");
    contentPanel.setAttribute("class", "tabPanelContentPanel");
    holder.appendChild(contentPanel);

    var activateTab = function(onLoadCallback) {

        var tab = this;

        for (var i = 0; i < headersContainer.children.length; i++) {
            var child = headersContainer.children[i];

            if (child === tab) {
                child.setAttribute("active-tab", "1");
            } else {
                child.setAttribute("active-tab", "0");
            }
        }
        
        var src = tab.getAttribute("content-src");

        if (src) {
            lt.fillBy(contentPanel, src, onLoadCallback);
        }

        var callback = tab["lt-onActivated"];
        if (callback) {
            callback(tab["lt-data-model"]);
        }
    };

    this.addTab = function(name, url, data, onActivated, id) {

        var tabDiv = document.createElement("div");
        tabDiv.setAttribute("class", "tabControlTabHead");

        if (url) {
            tabDiv.setAttribute("content-src", url);
        }

        tabDiv["lt-data-model"] = data;
        tabDiv["lt-onActivated"] = onActivated;
        tabDiv["lt-tab-id"] = id;
        headersContainer.appendChild(tabDiv);

        tabDiv.addEventListener("click", function() { activateTab.call(this, null); });

        var titleDiv = document.createElement("div");
        titleDiv.setAttribute("class", "tabControlHeadTitle");
        titleDiv.innerHTML = name;
        tabDiv.appendChild(titleDiv);
        
        if (self.autoselect && headersContainer.children.length === 1) {
            activateTab.call(headersContainer.children[0], null);
        }
    };

    self.selectTab = function(tabId, callback) {
        var targetTab = null;

        for (var i = 0; i < headersContainer.children.length; i++) {
            var child = headersContainer.children[i];
            if (child["lt-tab-id"] === tabId) {
                targetTab = child;
                break;
            }
        }

        if (targetTab == null) {
            throw new Error("Invalid tab id");
        }

        activateTab.call(targetTab, callback);
    };
};