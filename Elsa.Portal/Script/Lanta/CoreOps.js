var lt = lt || {};
lanta.CoreOps = lanta.CoreOps || {};

lanta.Extensions = lanta.Extensions || {};

lanta.Extensions.defaultErrorHandler = lanta.Extensions.defaultErrorHandler || function(error) {
    console.error(error);

    const text = (error.message || error || "?").toString();

    alert(text);
};

window.onbeforeunload = function () {
    //lanta.Extensions.defaultErrorHandler = function(e) {};
};

lanta.CoreOps.defaultCustomArgumentFactory = function (element, argumentName) {
    throw new Error("Cannot find element by \"" + argumentName + "\" inside of " + element);
};

lanta.Extensions.customArgumentFactory = lanta.Extensions.customArgumentFactory || lanta.CoreOps.defaultCustomArgumentFactory;

if (!lanta.CoreOps.elementSeekers) {
    var tagSeeker = function (target, tag, value) {
        var result = [];
        var childrenByTagName = getChildrenWithSelf(target);

        for (var i = 0; i < childrenByTagName.length; i++) {
            var child = childrenByTagName[i];
            var name = child.getAttribute(tag);
            if (name === value) {
                result.push(child);
            }
        }

        if (result.length === 0) {
            return null;
        }

        return result;
    };

    var seekers = [];
    seekers.push(function (target, id) { return tagSeeker(target, "id", id); });
    seekers.push(function (target, className) { return target.getElementsByClassName(className); });
    seekers.push(function (target, name) { return tagSeeker(target, "name", name); });
    seekers.push(function (target, ltName) { return tagSeeker(target, "lt-name", ltName); });
    seekers.push(lanta.Extensions.customArgumentFactory);

    lanta.CoreOps.elementSeekers = seekers;
}

lanta.CoreOps.seekElement = lanta.CoreOps.seekElement || function (root, elementName, doNotThrow) {
    var result = null;
    for (var i = 0; i < seekers.length; i++) {
        var seeker = seekers[i];

        if ((!!doNotThrow) && seeker === lanta.CoreOps.defaultCustomArgumentFactory) {
            continue;
        }

        result = seeker(root, elementName);

        if ((!result) || (result.length === 0)) {
            continue;
        }

        break;
    }

    if (!result) {
        return null;
    }

    if (result.length === 0) {
        return null;
    }

    if (result.length === 1) {
        result = result[0];
    }

    return result;
};

lanta.CoreOps.seekClosestElement = lanta.CoreOps.seekClosestElement || function (refElement, name) {

    for (; !!refElement; refElement = refElement.parentElement) {
        var result = lanta.CoreOps.seekElement(refElement, name, true);
        if (!!result) {

            if (Array.isArray(result) && (result.length > 0)) {
                return result[0];
            }

            return result;
        }
    }
    return null;
};

lanta.CoreOps.attachController = lanta.CoreOps.attachController || function (element, controllerFunction, viewModel) {

    try {

        controllerFunction = controllerFunction || function() {};

        element["lt_element_controllers"] = element["lt_element_controllers"] || [];
        element["lt_element_controllers"].push(controllerFunction);
        
        element.bind = function(handler) {
            var builder = new lanta.BindingCore.BindingBuilder(this, handler);
            builder.bind();
            return builder;
        };

        element.attribute = function (handler) {
            this["lt_attribute_bindings"] = element["lt_attribute_bindings"] || [];
            this["lt_attribute_bindings"].push(new lanta.AttributeBinding(this, handler));
        };

        element.bind.bind(element);

        lanta.Markup.process(element);

        lanta.FuncUtil.invoke(controllerFunction, element, lanta.CoreOps.seekElement);

        var bindingProcessor = element["lt_element_binding_processor"];
        if (!!bindingProcessor) {
            if (viewModel) {
                lt.setViewModel(element, viewModel);
            }

            bindingProcessor.activate();
        }
    } catch (e) {
        lanta.Extensions.defaultErrorHandler(e);
    }
};

lt.getViewModel = lt.getViewModel || function(element) {
    
    if (!element) {
        return null;
    }

    if (!!element["lt_view_model"]) {
        return element["lt_view_model"];
    }

    return lt.getViewModel(element.parentElement);
};

lt.setViewModel = lt.setViewModel || function(element, vm) {
    lanta.BindingCore.setViewModel(element, vm);
    lt.notify(element);
};