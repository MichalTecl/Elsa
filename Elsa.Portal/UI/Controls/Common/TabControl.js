var app = app || {};
app.ui = app.ui || {};
app.ui.TabControl = app.ui.TabControl || function(holder) {

    var self = this;

    var tabs = [];

    var headersContainer = document.createElement("div");
    headersContainer.setAttribute("class", "tabPanelHeadersContainer");
    holder.appendChild(headersContainer);

    var contentPanel = document.createElement("div");
    contentPanel.setAttribute("class", "tabPanelContentPanel");
    holder.appendChild(contentPanel);
    
    var activateTab = function() {

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

        lt.fillBy(contentPanel, src);
    };

    this.addTab = function(name, url) {

        var tabDiv = document.createElement("div");
        tabDiv.setAttribute("class", "tabControlTabHead");
        tabDiv.setAttribute("content-src", url);
        headersContainer.appendChild(tabDiv);

        tabDiv.addEventListener("click", activateTab);

        var titleDiv = document.createElement("div");
        titleDiv.setAttribute("class", "tabControlHeadTitle");
        titleDiv.innerHTML = name;
        tabDiv.appendChild(titleDiv);

        if (headersContainer.children.length === 1) {
            activateTab.call(headersContainer.children[0]);
        }
    };

};