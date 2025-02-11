var lanta = lanta || {};
lanta.ViewLoading = lanta.ViewLoading || {};
lanta.ViewLoading.cache = lanta.ViewLoading.cache || {};
lanta.ViewLoading.makeScriptsLive = lanta.ViewLoading.makeScriptsLive ||
    function (div, sourceUrl) {

        var allScripts = div.getElementsByTagName("script");

        for (var i = 0; i < allScripts.length; i++) {
            var sourceTag = allScripts[i];

            var newTag = document.createElement("script");
            var sourceAttributes = sourceTag.attributes;
            if (!!sourceAttributes) {
                for (var j = 0; j < sourceAttributes.length; j++) {
                    var sourceAttribute = sourceAttributes[j];
                    newTag.setAttribute(sourceAttribute.name, sourceAttribute.value);
                }
            }

            newTag.innerHTML = sourceTag.innerHTML + "\r\n//# sourceURL=" + sourceUrl;
            sourceTag.parentElement.insertBefore(newTag, sourceTag);
            sourceTag.parentElement.removeChild(sourceTag);
        }

    };

lanta.ViewLoading.inheritAttributes = lanta.ViewLoading.inheritAttributes || function(element, html) {

    var matches = html.match(/{attr\.[^}]+}/gim) || [];

    for (let i = 0; i < matches.length; i++) {
        var attributeName = matches[i].match(/\.[^}]+/gim)[0].substring(1);

        var sourceValue = element.getAttribute(attributeName) || "";

        html = html.split(matches[i]).join(sourceValue);
    }

    var binding = element.getAttribute("data-bind");
    if ((!binding) || (binding.length === 0)) {
        return html;
    }

    var expressions = binding.split(";");
    for (var expIndex = 0; expIndex < expressions.length; expIndex++) {
        var exp = expressions[expIndex];
        if (exp.trim().length === 0) {
            continue;
        }

        var parts = exp.split(":");
        if (parts.length !== 2) {
            var mess = "Invalid expression '" + exp + "'";

            console.error(mess);
            element.innerHTML = mess;
            if (!window['thrown' + mess]) {
                window['thrown' + mess] = true;
                throw new Error(mess);
            }
            
            return '<i>' + mess + '</i>';
        }

        var to = parts[0].trim();
        var from = parts[1].trim();

        html = html.split("$" + to + "$").join(from);
    }

    return html;
};

lanta.ViewLoading.loader = function (target, value, attribute, callback) {

    lt.api(value).useCache().ignoreDisabledApi().noJson().get(function (html) {
        
        if (html.indexOf("{%GENERATE%}") > -1) {
            var dynamicValue = new Date().getUTCMilliseconds() + "_" + Math.random();

            while (html.indexOf("{%GENERATE%}") > -1) {
                html = html.replace("{%GENERATE%}", dynamicValue);
            }
        }

        html = lanta.ViewLoading.inheritAttributes(target, html);

        var content = target.innerHTML;
        html = html.split("{content}").join(content);

        if (attribute === "fill-by") {
            target.innerHTML = html;
            lanta.ViewLoading.makeScriptsLive(target, value);
            if (!!callback) {
                callback(target);
            }
            return;
        }

        if (attribute === "replace-by") {
            var xdiv = document.createElement("DIV");
            xdiv.innerHTML = html;
            
            lanta.ViewLoading.makeScriptsLive(xdiv, value);

            var parent = target.parentElement;
            if (parent == null) {
                return;
            }

            var els = [];
            for (var nid = 0; nid < xdiv.childNodes.length; nid++) {
                els.push(xdiv.childNodes[nid]);
            }
            xdiv.innerHTML = '';
            for (var i = 0; i < els.length; i++) {

                try {
                    target.parentNode.insertBefore(els[i], target);
                }catch (ieieie){}
            }

            parent.removeChild(target);

            if (!!callback) {
                callback(els[0]);
            }
        }

    });

};

lt.watchCustomAttribute("fill-by", lanta.ViewLoading.loader);
lt.watchCustomAttribute("replace-by", lanta.ViewLoading.loader);

lt.replaceBy = lt.replaceBy || function (placeholder, src, callback) {
    lanta.ViewLoading.loader(placeholder, src, "replace-by", callback);
};

lt.fillBy = lt.fillBy || function (placeholder, src, callback) {
    lanta.ViewLoading.loader(placeholder, src, "fill-by", callback);
};