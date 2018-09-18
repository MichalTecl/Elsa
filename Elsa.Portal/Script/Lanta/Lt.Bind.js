var lt = lt || {};
lanta.BindingCore = lanta.BindingCore || {};

lanta.BindingCore.BindingBuilder = lanta.BindingCore.BindingBuilder || function(owner, handler) {

    var self = this;
    var bindingParams = [];

    var windowSourceFactory = function (owner, expression) {
        return window;
    };

    var findVmSourceFactory = function(owner, expression) {
        
        if (!owner) {
            return null;
        }

        if (!!owner["lt_view_model"]) {
            return owner["lt_view_model"];
        }

        return findVmSourceFactory(owner.parentElement, expression);
    };

    var canBeNullComparer = function(oldValue, newValue) {
        if (oldValue !== newValue) {
            return lanta.BindingCore.ComparerResult.CHANGED;
        }

        return lanta.BindingCore.ComparerResult.UNCHANGED;
    };

    this.canBeNullFunction = function(argument) {
        var param = this.getParamByArgument(argument);
        param.comparer = canBeNullComparer;
        this.updateBinding();
        return this;
    };

    this.relativeToVmFunction = function(argument) {
        var param = this.getParamByArgument(argument);
        param.sourceFactory = findVmSourceFactory;
        this.updateBinding();
        return this;
    };
    
    this.toGlobalFunction = function (expression, argument) {
        var param = this.getParamByArgument(argument);
        
        if (!!expression) {
            param.expression = expression;
        } else {
            param.expression = argument;
        }

        param.hasGlobalSource = true;
        param.sourceFactory = windowSourceFactory;
        
        return this;
    };

    this.relativeToGlobalFunction = function(expression, argument) {
        return this.toGlobalFunction(expression + "." + argument, argument);
    };

    this.getParamByArgument = function(argumentName) {
        for (var i = 0; i < bindingParams.length; i++) {
            var bp = bindingParams[i];
            if (bp.originalArgumentName === argumentName) {
                return bp;
            }
        }

        throw new Error("Argument " + argumentName + " does not exist");
    };

    this.relativeToGlobal = function(modelName) {
        for (var i = 0; i < bindingParams.length; i++) {
            var bp = bindingParams[i];
            if (bp.hasGlobalSource) {
                continue;
            }

            bp.expression = modelName + "." + bp.expression;
            bp.hasGlobalSource = true;
            bp.sourceFactory = windowSourceFactory;
        }

        return this;
    };

    this.updateBinding = function() {
        
    };

    this.bind = function() {

        var builder = self;

        var argumentNames = lanta.FuncUtil.getArgumentNames(handler);
        
        var rootPath = owner["lt-view-model-base"];

        for (var i = 0; i < argumentNames.length; i++) {
            var argumentName = argumentNames[i];
            var expression = (!!rootPath) ? rootPath + "." + argumentName : argumentName;
            var parameter = lanta.BindingCore.createParameter(expression);

            parameter.originalArgumentName = argumentName;
            bindingParams.push(parameter);
            
            builder[argumentName + "CanBeNull"] = (new Function(" return this.canBeNullFunction('" + argumentName + "'); ")).bind(builder);
            builder[argumentName + "ToGlobal"] = (new Function("expression", " return this.toGlobalFunction(expression, '" + argumentName + "'); ")).bind(builder);
            builder[argumentName + "RelativeToGlobal"] = (new Function("expression", " return this.relativeToGlobalFunction(expression, '" + argumentName + "'); ")).bind(builder);
            builder[argumentName + "RelativeToVm"] = (new Function("expression", " return this.relativeToVmFunction('" + argumentName + "'); ")).bind(builder);
        }

        lanta.BindingCore.bind(owner, handler, bindingParams);
        
        return builder;
    };
};