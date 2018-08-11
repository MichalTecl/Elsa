var lanta = lanta || {};
lanta.FuncUtil = lanta.FuncUtil || {};

lanta.FuncUtil.getArgumentNames = lanta.FuncUtil.getArgumentNames || function(func) {
    const stripComments = /((\/\/.*$)|(\/\*[\s\S]*?\*\/))/mg;
    const argumentNames = /([^\s,]+)/g;

    var fnStr = func.toString().replace(stripComments, '');
    var result = fnStr.slice(fnStr.indexOf('(') + 1, fnStr.indexOf(')')).match(argumentNames);
    if (result === null)
        result = [];
    return result;
};

lanta.FuncUtil.invoke = lanta.FuncUtil.invoke || function(fn, context, argumentValueFactory) {

    var argary = [];

    var argNames = lanta.FuncUtil.getArgumentNames(fn);



    for (var i = 0; i < argNames.length; i++) {
        var argName = argNames[i];
        argary.push(argumentValueFactory(context, argName));
    }

    return fn.apply(context, argary);
};