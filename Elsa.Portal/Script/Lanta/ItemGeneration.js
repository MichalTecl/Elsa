var lt = lt || {};
lanta.itemsGeneration = lanta.itemsGeneration || {};

lanta.itemsGeneration.Generator = lanta.itemsGeneration.Generator ||
    function() {

        var getChildByKey = function(container, key) {
            var children = container.children || container.childNodes;
            for (var i = 0; i < children.length; i++) {
                var child = children[i];
                if (child["lt-item-key"] === key) {
                    return child;
                }
            }

            return null;
        };

        var createChild = function(itemTemplate, key) {
            var clone = itemTemplate.cloneNode(true);
            clone["lt-item-key"] = key;
            clone.className = clone.className.replace(/\blt-template\b/g, "");
            return clone;
        };

        var placeTo = function(container, element, position) {

            var children = container.children || container.childNodes;

            if (position >= children.length - 1) {
                container.appendChild(element);
                return;
            }

            var nextChild = children[position];
            if (nextChild === element) {
                return;
            }

            container.insertBefore(element, nextChild);
        };

        this.generate = function(container, itemTemplate, collection, keyGenerator, controller) {

            try {

                var templateControllers = itemTemplate["lt_element_controllers"] || [];

                if ((collection === null) || (collection === undefined)) {
                    container.innerHTML = "";
                    return;
                }

                var keyIndex = {};

                for (var keyItemIndex in collection) {
                    if (!collection.hasOwnProperty(keyItemIndex)) continue;
                    var keyDataItem = collection[keyItemIndex];
                    var keykey = (!keyGenerator) ? keyItemIndex : keyGenerator(keyDataItem);

                    keyIndex[keykey] = 1;
                }

                var children = container.children || container.childNodes;

                for (var i = children.length - 1; i >= 0; i--) {
                    var ch = children[i];
                    var childKey = ch["lt-item-key"];

                    if (!keyIndex[childKey]) {
                        container.removeChild(ch);
                    }
                }

                var position = 0;
                for (var itemIndex in collection) {
                    if (!collection.hasOwnProperty(itemIndex)) continue;
                    var dataItem = collection[itemIndex];
                    var key = (!keyGenerator) ? position : keyGenerator(dataItem);

                    var attach = false;
                    var child = getChildByKey(container, key);
                    if (!child) {
                        attach = true;
                        child = createChild(itemTemplate, key);
                    }

                    placeTo(container, child, position);

                    if (attach) {

                        for (var tcindex = 0; tcindex < templateControllers.length; tcindex++) {
                            var templateController = templateControllers[tcindex];
                            lanta.CoreOps.attachController(child, templateController);
                        }

                        lanta.CoreOps.attachController(child, controller);
                    }

                    lanta.BindingCore.setViewModel(child, dataItem);

                    position++;
                }
            } catch (e) {
                lanta.Extensions.defaultErrorHandler(e);
            }
        };
    };

lanta.itemsGeneration.Generator.instance = lanta.itemsGeneration.Generator.instance || new lanta.itemsGeneration.Generator();

lanta.itemsGeneration.generate = lanta.itemsGeneration.generate || function(container, itemTemplate, collection, keyGenerator, controller) {
    lanta.itemsGeneration.Generator.instance.generate(container, itemTemplate, collection, keyGenerator, controller);
};

var lt = lt || {};
lt.generate = lt.generate || function(container, itemTemplate, collection, keyGenerator, controller) {
    lanta.itemsGeneration.Generator.instance.generate(container, itemTemplate, collection, keyGenerator, controller);
};