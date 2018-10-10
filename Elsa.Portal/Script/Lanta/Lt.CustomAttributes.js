var lanta = lanta || {};
lanta.CustomAttributes = lanta.CustomAttributes || {};
lanta.CustomAttributes.Watcher = lanta.CustomAttributes.Watcher || function() {

    const oldValueStorePrefix = "__lt_attribute_old_value_";

    //handler = function(element, newValue, attributeName, oldValue)
    var handlers = {};
    var initialized = false;

    var visitElement = function (element, attributes) {
        if (!element.hasAttributes()) {
            return;
        }

        var elementAttributes = element.attributes;
        attributes = attributes || [];
        if (attributes.length === 0) {
            for (var atrId = 0; atrId < elementAttributes.length; atrId++) {
                attributes.push(elementAttributes[atrId].name);
            }
        } else {
            var filteredAttributes = [];
            for (var eai = 0; eai < attributes.length; eai++) {
                if (element.hasAttribute(attributes[eai])) {
                    filteredAttributes.push(attributes[eai]);
                }
            }

            if (filteredAttributes.length === 0) {
                return;
            }

            attributes = filteredAttributes;
        }

        for (var i = 0; i < attributes.length; i++) {

            var handlerList = handlers[attributes[i]];
            if (!handlerList) {
                continue;
            }

            var oldValueMarker = oldValueStorePrefix + attributes[i];

            var value = element.getAttribute(attributes[i]);
            if (value === element[oldValueMarker]) {
                continue;
            }

            for (var handlerIndex = 0; handlerIndex < handlerList.length; handlerIndex++) {
                var handler = handlerList[handlerIndex];
                try {
                    handler(element, value, attributes[i], element[oldValueMarker] || null);
                } catch (e) {
                    console.error("Attribute handler " + handler + " failed");
                    console.error(e);
                }
            }

            element[oldValueMarker] = value;
        }
    };

    var visitAll = function() {
        var allElements = document.body.getElementsByTagName("*");
            for (var i = 0; i < allElements.length; i++) {
                var e = allElements[i];
                visitElement(e);
            }
    };

    var watch = function () {

        if ((!document) || (!document.body)) {
            setTimeout(watch, 50);
            return;
        }

        document.addEventListener('DOMContentLoaded', function () { visitAll(); }, false);

        if (!initialized) {
            visitAll();
        }

        initialized = true;

        var config = { attributes: true, childList: true, subtree: true };

        var callback = function (mutationsList) {
            for (var i = 0; i < mutationsList.length; i++) {
                var mutation = mutationsList[i];
                if (mutation.type === 'attributes') {
                    visitElement(mutation.target, [mutation.attributeName]);
                }
            }
        };

        var observer = new MutationObserver(callback);
        observer.observe(document.body, config);
    };
    
    this.registerHandler = function(attributeName, handler) {

        var handlerList = handlers[attributeName] || (handlers[attributeName] = []);
        handlerList.push(handler);

        if (handlerList.length === 1) {
            watch();
        }

        if (!document || !document.body) {
            return;
        }

        var allElements = document.body.getElementsByTagName("*");
        for (var i = 0; i < allElements.length; i++) {
            var e = allElements[i];
            visitElement(e, [attributeName]);
        }
    };

    
};
lanta.CustomAttributes.Watcher.instance = lanta.CustomAttributes.Watcher.instance ||
    new lanta.CustomAttributes.Watcher();

var lt = lt || {};
lt.watchCustomAttribute = lt.watchCustomAttribute || function(attributeName, handler) {
    lanta.CustomAttributes.Watcher.instance.registerHandler(attributeName, handler);
};