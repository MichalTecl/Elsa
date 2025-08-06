function getChildrenWithSelf(root) {
    var result = [];

    if (!!root) {
        result.push(root);
        var children = root.getElementsByTagName("*");
        for (var i = 0; i < children.length; i++) {
            result.push(children[i]);
        }
    }

    return result;
}

var lt = lt || {};
lt.findParentAttribute = lt.findParentAttribute || ((currentElement, attributeName) => {

    while (currentElement) {
        if (currentElement.hasAttribute && currentElement.hasAttribute(attributeName)) {
            return currentElement.getAttribute(attributeName);
        }
        currentElement = currentElement.parentElement;
    }

    return null
});