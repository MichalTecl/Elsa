var lanta = lanta || {};
lanta.BindingCore = lanta.BindingCore || {};

lanta.BindingCore.subscribeNotificationsCallback = lanta.BindingCore.subscribeNotificationsCallback ||
    function(owner, callback) {
        
        if (!owner.ltNotificationListeners) {
            owner.ltNotificationListeners = [];
        }

        owner.ltNotificationListeners.push(callback);
    };

lanta.BindingCore.NotificationManager = lanta.BindingCore.NotificationManager || function() {

    var self = this;
    var queue = [];
    var timer = null;

    var tick = function() {

        var startTime = new Date();

        while (queue.length > 0) {
            
            if ((new Date() - startTime) > 30) {
                return;
            }

            var current = queue.shift();

            if (!!current.element.notifyModelChange) {
                current.element.notifyModelChange(current.notificationName);
            }

            if (!!current.element.ltNotificationListeners) {
                var ary = current.element.ltNotificationListeners;
                for (var x = 0; x < ary.length; x++) {
                    var callback = ary[x];
                    callback(current.notificationName);
                }
            }

            var children = current.element.children || current.element.childNodes;

            for (var i = 0; i < children.length; i++) {
                queue.push({ element: children[i], notificationName: current.notificationName });
            }
        }

        window.clearInterval(timer);
        timer = null;
    };

    var enqueue = function(owner, name) {
        queue.push({ element: owner, notificationName: name });

        if (queue.length === 1) {

            if (timer !== null) {
                window.clearInterval(timer);
                timer = null;
            }

            timer = window.setInterval(tick, 50);
        }
    };

    self.enqueueNotification = function (name) {
        enqueue(window.document, name);
    };

    self.notifyElement = function(element) {
        enqueue(element, "");
    };
};

lanta.BindingCore.NotificationManager.instance = lanta.BindingCore.NotificationManager.instance || new lanta.BindingCore.NotificationManager();

lanta.BindingCore.notify = lanta.BindingCore.notify || function (target) {

    try {
        if (target instanceof HTMLElement) {
            lanta.BindingCore.NotificationManager.instance.notifyElement(target);
            return;
        }
    } catch (ee) {
        console.error(ee);
    }

    lanta.BindingCore.NotificationManager.instance.enqueueNotification(target);
};

lanta.BindingCore.setViewModel = lanta.BindingCore.setViewModel || function(element, viewModel) {
    var processor = element["lt_element_binding_processor"];
    if (!processor) {
        return;
    }

    processor.setViewModel(viewModel);
};

lanta.BindingCore.ComparerResult = lanta.BindingCore.ComparerResult || { FORBIDDEN:2, CHANGED:1, UNCHANGED:0 };

lanta.BindingCore.defaultRelativeSourceFactory = lanta.BindingCore.defaultRelativeSourceFactory || function(owner, expression) {
    //return owner["lt_view_model"];
    // EXPERIMENT:

    return lt.getViewModel(owner);

};

lanta.BindingCore.defaultExpressionEvaluator = lanta.BindingCore.defaultExpressionEvaluator || function(owner, expression, relativeSourceFactory) {
    var parts = expression.split(".");
    
    var parentExp = relativeSourceFactory(owner, expression);
    for (var i = 0; i < parts.length; i++) {

        if (parts[i] === null || parts[i] === undefined || parts[i].length === 0) {
            continue;
        }

        if (parentExp === null || parentExp === undefined) {
            return parentExp;
        }
        parentExp = parentExp[parts[i]];
    }

    return parentExp;
};

lanta.BindingCore.defaultComparer = lanta.BindingCore.defaultComparer || function(oldValue, newValue) {
    
    if (oldValue === undefined) oldValue = null;
    if (newValue === undefined) newValue = null;

    if (newValue == null && oldValue == null) {
        return lanta.BindingCore.ComparerResult.FORBIDDEN;
    }

    if (newValue === oldValue) {
        return lanta.BindingCore.ComparerResult.UNCHANGED;
    }

    return lanta.BindingCore.ComparerResult.CHANGED;
};

lanta.BindingCore.createParameter = lanta.BindingCore.createParameter || function(expression, relativeSourceFactory, comparer, expressionEvaluator) {

    relativeSourceFactory = relativeSourceFactory || lanta.BindingCore.defaultRelativeSourceFactory;
    comparer = comparer || lanta.BindingCore.defaultComparer;
    expressionEvaluator = expressionEvaluator || lanta.BindingCore.defaultExpressionEvaluator;

    return {
        expression: expression,
        comparer: comparer,
        evaluator: expressionEvaluator,
        sourceFactory: relativeSourceFactory
    };
};

lanta.BindingCore.bind = lanta.BindingCore.bind || function(element, func, parameters) {

    if (!parameters.length) {
        throw new Error("Array required");
    }

    var processor = element["lt_element_binding_processor"];
    if (!processor) {
        processor = new lanta.BindingCore.ElementBindingProcessor(element);
        element["lt_element_binding_processor"] = processor;
    }

    processor.addBinding(func, parameters);
};

lanta.BindingCore.ElementBindingProcessor = lanta.BindingCore.ElementBindingProcessor || function(owner) {

    var bindings = [];
    var active = false;
    
    this.addBinding = function (func, parameters) {
        var newBinding = new lanta.BindingCore.ElementBinding(owner, func, parameters);
        bindings.push(newBinding);
    };
  
    var notifyModelChange = function (expression) {

        if (!active) {
            return;
        }

        for (var i = 0; i < bindings.length; i++) {
            bindings[i].notify(expression);
        }
    };

    this.setViewModel = function(viewModel) {
        for (var i = 0; i < bindings.length; i++) {
            bindings[i].notify(null, viewModel);
        }
    };

    this.activate = function() {
        active = true;
        notifyModelChange("");
    };

    this.notify = this.activate;
    
    owner.notifyModelChange = notifyModelChange;

};

lanta.BindingCore.ElementBinding = lanta.BindingCore.ElementBinding || function(owner, handler, parameters) {

    var oldValues = [];
    var oldValuesSerialized = [];

    var serialize = function(x) {
        if (x === undefined) x = null;
        if (x === null)
            return null;

        return JSON.stringify(x);
    };
    
    this.notify = function(expression, viewModel) {

        try {

            if (expression === undefined) expression = null;
            if (viewModel === undefined) viewModel = null;

            if ((expression !== null) && (viewModel !== null)) {
                throw new Error("Cannot notify by expression and either ViewModel");
            }

            if (viewModel !== null) {
                owner["lt_view_model"] = viewModel;
            }
            
            var result = lanta.BindingCore.ComparerResult.UNCHANGED;

            for (var j = 0; j < parameters.length; j++) {
                var param = parameters[j];

                var newValue = param.evaluator(owner, param.expression, param.sourceFactory);

                var serializedOldValue = oldValuesSerialized[j];
                var serializedNewValue = serialize(newValue);

                var comparationResult = param.comparer(serializedOldValue, serializedNewValue);

                oldValuesSerialized[j] = serializedNewValue;

                if (comparationResult > result) {
                    result = comparationResult;
                }

                oldValues[j] = newValue;
            }

            if (result !== lanta.BindingCore.ComparerResult.CHANGED) {
                return;
            }

            handler.apply(owner, oldValues);
        } catch (e) {
            lanta.Extensions.defaultErrorHandler(e);
        }
    };
};



var lt = lt || {};

lt.notify = lanta.notify || function(target) {
    try {
        target = target || "";
        lanta.BindingCore.notify(target);
    } catch (e) {
        lanta.Extensions.defaultErrorHandler(e);
    }
};

lt.updateBindings = lt.updateBindings || function (element) {
    var processor = element["lt_element_binding_processor"];
    if (!!processor) {
        processor.notify("");
    }
};