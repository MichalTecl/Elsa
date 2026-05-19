lanta.ControlBuilder = function() {

    var self = this;
    var elements = [];

	
    this.element = function (e) {
        if ((typeof e) === "string") {
        	var ebid = document.getElementById(e);
        	if (!ebid) {

	            ebid = document.querySelector(e);

	            if (!ebid) {
	                throw new Error("Element '" + e + "' not found");
	            }
	        }
			elements.push(ebid);
			return self;
        }

        if (e.nodeType > 0) {
        	elements.push(e);
            return self;
        }

		if (e.length !== undefined) {
		    if (e.length !== 1) {
		        throw new Error("Passed array has length=" + e.length);
		    }

		    return self.element(e[0]);
		}

        throw new Error("lt.element expects an element, elementId, css selector or element[], elementId[], selector[]. Invalid value passed:" + e);
    };

    this.elements = function (e) {
    	if ((typeof e) === "string") {
    		var ebid = document.querySelectorAll(e);
			if ((!ebid) || (ebid.length === 0)) {
			    throw new Error("No elements returned by selector '" + e + "'");
			}

			for (var i = 0; i < ebid.length; i++) {
			    self.element(ebid[i]);
			}

    		return self;
    	}
		
    	if (e.length !== undefined) {
    		if (e.length === 0) {
    			throw new Error("Passed array is empty");
    		}

			for (var j = 0; j < e.length; j++) {
			    self.element(e[j]);
			}

	        return self;
	    }

    	throw new Error("lt.elements expects a selector, element[], elementId[], selector[]. Invalid value passed:" + e);
    };

    this.withModel = function (m) {

        var modelParam = lanta.BindingCore.createParameter(m,
            function(owner, expression) {
                return window;
            });

        var resolveModel = function(expression) {
            return lanta.BindingCore.defaultExpressionEvaluator(window, expression, function() { return window; });
        };
        
        for (var i = 0; i < elements.length; i++) {
        	var element = elements[i];

            lanta.BindingCore.bind(element, function(value) {
            	this["lt_view_model"] = value;

                if (value !== null && value !== undefined && !!this["lt_model_wait_handle"]) {
                    window.clearInterval(this["lt_model_wait_handle"]);
                    this["lt_model_wait_handle"] = null;
                }

                lt.updateBindings(this);
            }, [modelParam]);

            if (!element["lt_model_wait_handle"]) {
                (function(boundElement, expression) {
                    var attempts = 0;
                    boundElement["lt_model_wait_handle"] = window.setInterval(function() {
                        attempts++;

                        var resolvedModel = resolveModel(expression);
                        if (resolvedModel !== null && resolvedModel !== undefined) {
                            boundElement["lt_view_model"] = resolvedModel;
                            window.clearInterval(boundElement["lt_model_wait_handle"]);
                            boundElement["lt_model_wait_handle"] = null;
                            lt.updateBindings(boundElement);
                            return;
                        }

                        if (attempts > 200) {
                            window.clearInterval(boundElement["lt_model_wait_handle"]);
                            boundElement["lt_model_wait_handle"] = null;
                        }
                    }, 25);
                })(element, m);
            }
        }

        return this;
    };

    this.attach = function(controller) {

        if (!controller.hasOwnProperty('prototype'))
            throw new Error("Arrow function not allowed here!");

        controller = controller || function() {};

        var model = null;

        for (var i = 0; i < elements.length; i++) {

            var element = elements[i];

            var eModel = element.getAttribute("data-model") || null;
			if (i > 0 && model !== eModel) {
				throw new Error("Mismatch data-model values of attaching elements");
			}
            model = eModel;
			
			if (model !== null) {
			    this.withModel(model);
			}

            lanta.CoreOps.attachController(elements[i], controller);
        }
		
        return { attach: self.attach };
    };

    this.attachModel = function (controller) {

        if (!controller.hasOwnProperty('prototype'))
            throw new Error("Arrow function not allowed here!");

        if (elements.length !== 1)
            throw new Error("attachModel requires exactly one element");

        var element = elements[0];

        try {

            controllerFunction = controller || function () { };

            element["lt_element_controllers"] = element["lt_element_controllers"] || [];
            element["lt_element_controllers"].push(controllerFunction);

            element.bind = function (handler) {
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

            const model = element;

            var bindingProcessor = element["lt_element_binding_processor"];
            if (!!bindingProcessor) {
                lt.setViewModel(element, model);
                
                bindingProcessor.activate();
            }
        } catch (e) {
            lanta.Extensions.defaultErrorHandler(e);
        }
    };
};

lt.element = function(e) {
	var builder = new lanta.ControlBuilder();
    return builder.element(e);
};

lt.elements = function(e) {
	var builder = new lanta.ControlBuilder();
    return builder.elements(e);
};

