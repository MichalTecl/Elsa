var lanta = lanta || {};
lanta.AttributeBinding = lanta.AttributeBinding || function(owner, handler) {
    var arg = lanta.FuncUtil.getArgumentNames(handler);
    if (arg.length !== 1) {
        throw new Error("Attribute bound function must have one argument: " + handler.toString());
    }

    var attributeName = arg[0];

    var find = function() {
        
        var element = owner;

        while (!!element) {
            var value = null;
            if (element.getAttribute && (!!(value = element.getAttribute(attributeName)))) {

                if (value === "null") {
                    value = null;
                }

                if (owner["lastAttributeBindingValueFor_" + attributeName] === value) {
                    return;
                }

                handler(value);
                owner["lastAttributeBindingValueFor_" + attributeName] = value;
                return;
            }

            element = element.parentElement;
        }

        setTimeout(find, Math.floor(Math.random() * 100) + 1);
    };

    setTimeout(find, 0);

    lanta.BindingCore.subscribeNotificationsCallback(owner, find);

};