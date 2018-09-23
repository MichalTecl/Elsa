var lanta = lanta || {};
lanta.Markup = lanta.Markup || {};

lanta.Markup.attributeSetters = lanta.Markup.attributeSetters || [];

lanta.Markup.elementMarkerCounter = lanta.Markup.elementMarkerCounter || 0;

if (lanta.Markup.attributeSetters.length === 0) {

	// set attribute
	lanta.Markup.attributeSetters.push(function (target, propertyName, value) {
	    target.setAttribute(propertyName, value);
	    target[propertyName] = value;
	    return true;
	});

	//style setter
	lanta.Markup.attributeSetters.push(function (target, propertyName, value) {

		if (propertyName.indexOf("style.") !== 0) {
		    return false;
		}

		var props = propertyName.split('.');

		var currP = target["style"];
		if (!currP) {
		    target["style"] = currP = {};
		}

		for (var i = 1; i < props.length - 1; i++) {
		    currP = currP[props[i]];
		}

	    currP[props[props.length - 1]] = value;
		
		return true;
	});

    //class
	lanta.Markup.attributeSetters.push(function (target, propertyName, value) {

	    if (propertyName.indexOf("class.") !== 0) {
	        return false;
	    }

	    var className = propertyName.substring(6);

	    var currentClasses = target.className.split(" ");
	    var newClasses = [];

        for (var i = 0; i < currentClasses.length; i++) {
            if (currentClasses[i] !== className) {
                newClasses.push(currentClasses[i]);
            }
        }

        if (value) {
            newClasses.push(className);
        }

        target.setAttribute("class", newClasses.join(" "));

	    return true;
	});

    //not_class
	lanta.Markup.attributeSetters.push(function (target, propertyName, value) {

	    if (propertyName.indexOf("class!.") !== 0) {
	        return false;
	    }

	    var className = propertyName.substring(7);

	    var currentClasses = target.className.split(" ");
	    var newClasses = [];

	    for (var i = 0; i < currentClasses.length; i++) {
	        if (currentClasses[i] !== className) {
	            newClasses.push(currentClasses[i]);
	        }
	    }

	    if (!value) {
	        newClasses.push(className);
	    }

	    target.setAttribute("class", newClasses.join(" "));

	    return true;
	});

	//checked, selected, disabled
	lanta.Markup.attributeSetters.push(function (target, propertyName, value) {

		if (propertyName !== "checked" && propertyName !== "selected" && propertyName !== "disabled") {
			return false;
		}

		if (!!value) {
		    target.setAttribute(propertyName, value);
		} else {
		    target.removeAttribute(propertyName);
		}

	    target[propertyName] = value;

		return true;
	});

	//text setter
	lanta.Markup.attributeSetters.push(function (target, propertyName, value) {

		if (propertyName !== "text") {
			return false;
		}

		var text = document.createTextNode(value);
		var p = document.createElement('p');
		p.appendChild(text);
		target.innerHTML = p.innerHTML;

	    return true;
	});


};

lanta.Markup.getElementMarker = lanta.Markup.getElementMarker || function(element) {
    var marker = element["lt-autobind-marker"];
	if (!!marker) {
	    return marker;
	}

    lanta.Markup.elementMarkerCounter++;
    marker = "mrk_" + lanta.Markup.elementMarkerCounter;

    element["lt-autobind-marker"] = marker;

    return marker;
};

lanta.Markup.getChildByMarker = lanta.Markup.getChildByMarker || function(root, marker) {

    var allChildren = getChildrenWithSelf(root);

	for (var i = 0; i < allChildren.length; i++) {
		var e = allChildren[i];
		if (e["lt-autobind-marker"] === marker) {
		    return e;
		}
	}

    return null;
};

lanta.Markup.setValue = lanta.Markup.setValue || function(target, propertyName, value) {
    for (var i = lanta.Markup.attributeSetters.length - 1; i >= 0; i--) {
    	var setter = lanta.Markup.attributeSetters[i];
		if (setter(target, propertyName, value)) {
		    return;
		}
    }
};

lanta.Markup.bindExpression = lanta.Markup.bindExpression || function(owner, element, expression) {

	var expressions = expression.split(";");

	for (var expIndex = 0; expIndex < expressions.length; expIndex ++) {
		var exp = expressions[expIndex];

		var parts = exp.split(":");
		if (parts.length !== 2) {
		    throw new Error("Invalid expression '" + exp + "'");
		}

		var to = parts[0].trim();
		var from = parts[1].trim();

		var param = lanta.BindingCore.createParameter(from);

	    var marker = lanta.Markup.getElementMarker(element);
	    var fn = new Function(["_value"], " var target = lanta.Markup.getChildByMarker(this, '" + marker + "'); lanta.Markup.setValue(target, '" + to + "', _value);\r\n//# sourceURL="+to+"_"+from+".js");

	    lanta.BindingCore.bind(owner, fn, [param]);
	}
};

lanta.Markup.bindItemsSourceExpression = lanta.Markup.bindItemsSourceExpression || function (owner, element, expression) {
    
    var param = lanta.BindingCore.createParameter(expression);

    var marker = lanta.Markup.getElementMarker(element);

    var children = element.children || element.childNodes;

    var itemTemplate = children[0];
    if (!itemTemplate) {
        throw new Error("Element " +
            element.toString() +
            " has items-source, but doesn't have any child element, which is required to be an item template");
    }

    var keyGeneratorLambda = element.getAttribute("item-key");
    if (!keyGeneratorLambda) {
        keyGeneratorLambda = "null";
    } else {
        keyGeneratorLambda = "function(VM){ return " + keyGeneratorLambda + "; }";
    }
    

    element["lt-item-template"] = itemTemplate;

    var fn = new Function(["_value"],
        " var container = lanta.Markup.getChildByMarker(this, '" + marker + "');\r\n"
        + "var template = container['lt-item-template']; \r\n if(!template) { throw new Error('No item template found'); } \r\n"
        + "lanta.itemsGeneration.Generator.instance.generate(container, template, _value, " + keyGeneratorLambda + ");\r\n"
        + "\r\n//# sourceURL=" + element.toString() + "_itemsGeneration.js");

    lanta.BindingCore.bind(owner, fn, [param]);
    
};

lanta.Markup.bindEventExpression = lanta.Markup.bindEventExpression || function(owner, element, expression) {
    
	var expressions = expression.split(";");

	for (var expIndex = 0; expIndex < expressions.length; expIndex++) {
	    var exp = expressions[expIndex];
        if (exp === null || exp.length === 0) {
            continue;
        }

		var parts = exp.split(":");
		if (parts.length !== 2) {
		    console.warn("Invalid expression '" + exp + "'");
            continue;
		}

		var eventType = parts[0].trim();
		var handler = parts[1].trim();

		var handlerParts = handler.split("(").join(",").split(")").join(",").split(",");

		for (var i = 0; i < handlerParts.length; i++) {
		    handlerParts[i] = handlerParts[i].trim();
		}
		
		if (handlerParts.length < 1) {
		    continue;
		}

		var eMarker = lanta.Markup.getElementMarker(element);
		var method = handlerParts.shift();

		var params = [];
        for (var j = 0; j < handlerParts.length; j++) {
        	var expr = handlerParts[j].trim();
            if ((!expr) || (expr.length < 1)) {
                continue;
            }
            params.push(expr);
        }

        var eventInfo = JSON.stringify({ source: eMarker, method: method, arguments: params });

	    eventInfo = eventInfo.split("'").join("\\'");

	    var fnCode = "lanta.Markup.fireEvent(event, JSON.parse('" + eventInfo + "'))";
	    element.addEventListener(eventType,  new Function(["event"], fnCode));
	}


};

lanta.Markup.fireEvent = lanta.Markup.fireEvent || function(event, eventInfo) {

    try {

        var handler = null;
        var elementOwner = event.srcElement;

        var vm = null;
        var viewModelOwner = event.srcElement;
        for (; !!viewModelOwner; viewModelOwner = viewModelOwner.parentElement) {
            vm = viewModelOwner["lt_view_model"];
            if (!!vm) {
                break;
            }
        }

        var args = [];
        for (var argumentIndex = 0; argumentIndex < eventInfo.arguments.length; argumentIndex++) {

            var argumentVm = vm;

            var argumentExpression = eventInfo.arguments[argumentIndex];

            if (argumentExpression === "true") {
                args.push(true);
                continue;
            } else if (argumentExpression === "VM") {
                args.push(argumentVm);
                continue;
            }
            else if (argumentExpression === "false") {
                args.push(false);
                continue;
            } else if (argumentExpression === "this") {
                args.push(event.srcElement);
                continue;
            } else if (argumentExpression === "event") {
                args.push(event);
                continue;
            } else if (argumentExpression.indexOf("'") === 0 || argumentExpression.indexOf('"') === 0) {

                argumentExpression = argumentExpression.substring(1);
                if (argumentExpression[argumentExpression.length - 1] === '"' ||
                    argumentExpression[argumentExpression.length - 1] === "'") {
                    argumentExpression = argumentExpression.substring(0, argumentExpression.length - 1);
                }
                args.push(argumentExpression);
                continue;
            } else if (!isNaN(argumentExpression)) {
                args.push(Number.parseFloat(argumentExpression));
                continue;
            } else if (argumentExpression === "null") {
                args.push(null);
                continue;
            } else if (argumentExpression.indexOf("this.") === 0) {
                argumentExpression = argumentExpression.substring(5);
                argumentVm = event.srcElement;
            } else if (argumentExpression.indexOf("$") === 0) {
                var elmName = argumentExpression.substring(1);
                var argParts = elmName.split(".");
                if (!viewModelOwner) {
                    console.error("Cannot locate $" + argParts[0] + "because the root element was not found");
                }
                var element = lanta.CoreOps.seekClosestElement(event.srcElement, argParts[0]);
                if (!element) {
                    console.error("Cannot locate $" + argParts[0]);
                }
                argumentVm = element;
                argParts.shift();
                argumentExpression = argParts.join(".");
            }

            if (argumentVm === null || argumentVm === undefined) {
                args.push(null);
                continue;
            }

            var obtained = lanta.BindingCore.defaultExpressionEvaluator(viewModelOwner,
                argumentExpression,
                function() {
                    return argumentVm;
                });

            args.push(obtained);
        }

        if (eventInfo.method.indexOf("VM.") === 0) {
            if (vm === null || vm === undefined) {
                console.warn("Cannot invoke " + eventInfo.method + " because VM is null");
                return;
            }

            var methodExpression = eventInfo.method.substring(3);

            handler = lanta.BindingCore.defaultExpressionEvaluator(null, methodExpression, function() { return vm; });
            if (!handler) {
                console.error("Cannot invoke " + eventInfo.method + " because the method was not found");
                return;
            }

            elementOwner = vm;

        } else if (eventInfo.method.indexOf("window.") === 0) {
            var methodExpression = eventInfo.method.substring(7);
            handler = lanta.BindingCore
                .defaultExpressionEvaluator(null, methodExpression, function() { return window; });
            if (!handler) {
                console.error("Cannot invoke " + eventInfo.method + " because the method was not found");
                return;
            }
        } else {
            for (; !!elementOwner; elementOwner = elementOwner.parentElement) {

                handler = elementOwner[eventInfo.method];
                if (!handler || !handler.apply || !handler.call) {
                    continue;
                }

                break;
            }
        }

        if (!handler) {
            throw new Error("Event handler '" + eventInfo.method + "' not found");
        }

        handler.apply(elementOwner, args);
    } catch (e) {
        lanta.Extensions.defaultErrorHandler(e);
    }
};

lanta.Markup.process = lanta.Markup.process || function(owner) {

    try {

        var allChildren = getChildrenWithSelf(owner);

        for (var i = 0; i < allChildren.length; i++) {
            var child = allChildren[i];

            var dataBindExp = child.getAttribute("data-bind");
            if (!!dataBindExp) {
                lanta.Markup.bindExpression(owner, child, dataBindExp);
            }

            var eventExp = child.getAttribute("event-bind");
            if (!!eventExp) {
                lanta.Markup.bindEventExpression(owner, child, eventExp);
            }

            var itemsSourceExp = child.getAttribute("items-source");
            if (!!itemsSourceExp) {
                lanta.Markup.bindItemsSourceExpression(owner, child, itemsSourceExp);
            }
        }
    } catch (e) {
        lanta.Extensions.defaultErrorHandler(e);
    }
};
