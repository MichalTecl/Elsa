var lt = lt || {};



lt.updateModelByForm = lt.updateModelByForm || function(model, formRoot) {

    model = model || {};

    const children = getChildrenWithSelf(formRoot);
    for (let i = 0; i < children.length; i++) {
        const element = children[i];

        const attribute = element.getAttribute("update-model");
        if (!attribute) {
            continue;
        }

        var updater = element.updateModel || lt.updateModelByForm.defaultUpdater;

        const parts = attribute.split(";");
        for (let j = 0; j < parts.length; j++) {
            var expression = parts[j].trim();
            if (expression.length < 1) {
                continue;
            }

            var targetAndSource = expression.split(":");
            if (targetAndSource.length !== 2) {
                if (targetAndSource.length === 1) {
                    targetAndSource.push("value");
                } else {
                    throw new Error("Invalid update-model expression '" + expression + "'. Target:Source expected");
                }
            }

            updater.call(element, model, targetAndSource[0].trim(), targetAndSource[1].trim());
        }
    }

    return model;
};

lt.updateModelByForm.defaultUpdater = lt.updateModelByForm.defaultUpdater || function(model, targetPropertyName, sourcePropertyName) {
    model[targetPropertyName] = this[sourcePropertyName];
};