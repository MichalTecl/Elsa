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