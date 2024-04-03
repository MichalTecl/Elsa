using System;
using System.Collections.Generic;
using System.Reflection;

using CodeGeneration;
using CodeGeneration.Primitives;

using Robowire.RoboApi.Extensibility;

namespace Robowire.RoboApi.Convention.Default
{
    public class DefaultCallBuilder : IControllerMethodCallBuilder
    {
        public void BuildCall(
            IClassBuilder proxy,
            IMethodBuilder executeMethod,
            INamedReference contextParameter,
            Type controllerType,
            Func<Type, INamedReference> privateObjectsFactory,
            INamedReference interceptorField)
        {
            //public void Execute(RequestContext requestContext)

            var methodNameVar =
                executeMethod.Body.Var(
                    asg =>
                        asg.Write(typeof(MethodNameExtractor))
                            .Write(".ExtractMethodName(")
                            .Write(contextParameter)
                            .Write(")"));

            var resultVar = executeMethod.Body.DeclareLocal("__methodCallResult", typeof(object));
            executeMethod.Body.Assign(resultVar, x => x.Write("null"));

            foreach (var method in controllerType.GetMethods())
            {
                if ((method.DeclaringType != controllerType) && (!Attribute.IsDefined(method, typeof(CanBeInheritedAttribute))))
                {
                    continue;
                }

                var methodKey = method.Name.ToLowerInvariant();

                executeMethod.Body.If(
                    c => c.Compare(methodNameVar, b => b.String(methodKey)),
                    then => ImplementCall(controllerType, method, contextParameter, resultVar, privateObjectsFactory, proxy, interceptorField, then));
            }

            executeMethod.Body.Write("throw new ")
                .Write(typeof(InvalidOperationException))
                .Write("(")
                .String("Unknown method: ").Write(" + ").Write(methodNameVar)
                .Write(")")
                .EndStatement();
        }

        protected virtual void ImplementCall(
            Type controllerType,
            MethodInfo method,
            INamedReference contextParameter,
            INamedReference resultVar,
            Func<Type, INamedReference> privateObjectsFactory,
            IClassBuilder proxyBuilder,
            INamedReference interceptorField,
            ICodeBlockBuilder block)
        {

            var argumentsArray = block.Var(ini => ini.Write($"new System.Object[{method.GetParameters().Length}]"));

            block.Write("try {\r\n");

            var methodInfoField =
                proxyBuilder.HasField(typeof(MethodInfo))
                    .WithModifier("static")
                    .WithModifier("private")
                    .WithModifier("readonly")
                    .WithAssignment(
                        asg =>
                            asg.Write(typeof(ReflectonWrapper))
                                .Write(".")
                                .Write(nameof(ReflectonWrapper.GetMethodInfo))
                                .Write("(")
                                .Typeof(controllerType)
                                .Write(",")
                                .String(method.Name)
                                .Write(")"));
            
            var paramVars = new List<INamedReference>(method.GetParameters().Length);

            var parameterIndex = 0;
            foreach (var param in method.GetParameters())
            {
                var paramInfoField =
                    proxyBuilder.HasField(typeof(ParameterInfo))
                        .WithModifier("static")
                        .WithModifier("private")
                        .WithModifier("readonly")
                        .WithAssignment(
                            asg =>
                                asg.Write(typeof(ReflectonWrapper))
                                    .Write(".")
                                    .Write(nameof(ReflectonWrapper.GetParameterInfo))
                                    .Write("(")
                                    .Typeof(controllerType)
                                    .Write(",")
                                    .String(method.Name)
                                    .Write(",")
                                    .String(param.Name)
                                    .Write(")"));

                
                var paramReaderType = GetParameterReaderType(param);
                var readerField = privateObjectsFactory(paramReaderType);

                var paramVar = block.Var(asg => ComposeParameterRead(asg, readerField, param, methodInfoField, paramInfoField, contextParameter, interceptorField));

                paramVars.Add(paramVar);

                block.Write(argumentsArray).Write($"[{parameterIndex}] = ").Write(paramVar).EndStatement();

                parameterIndex++;
            }


            var isVoid = method.ReturnType == typeof(void);
            var resultWriterField = privateObjectsFactory(GetResultWriterType(method));

            block.Invoke(
                interceptorField,
                nameof(IControllerInterceptor.Call),
                args =>
                    args.WithParam("this")
                        .WithParam(methodInfoField)
                        .WithParam(contextParameter)
                        .WithParam(argumentsArray)
                        .WithParam(isVoid ? "true" : "false")
                        .WithParam(
                            call =>
                                {
                                    call.Write("() => {");

                                    if (!isVoid) call.Write("return ");

                                    call.Invoke(
                                        method.Name,
                                        argu =>
                                            {
                                                foreach (var argument in paramVars)
                                                {
                                                    argu.WithParam(argument);
                                                }
                                            }).EndStatement();

                                    if (isVoid)
                                    {
                                        call.Write("return null").EndStatement();
                                    }

                                    call.Write(" }");

                                }).WithParam(
                            result =>
                                {
                                    result.Write("__r => ")
                                        .Invoke(
                                            resultWriterField,
                                            nameof(IResultWriter.WriteResult),
                                            rinvo =>
                                                rinvo.WithParam(methodInfoField)
                                                    .WithParam(contextParameter)
                                                    .WithParam("__r")
                                                    .WithParam(isVoid ? "true" : "false"));
                                })).EndStatement();



            block.Write("} catch(System.Exception callException) {\r\n");

            block.Invoke(
                interceptorField,
                nameof(IControllerInterceptor.OnException),
                arguments =>
                    arguments.WithParam("this")
                        .WithParam(methodInfoField)
                        .WithParam(contextParameter)
                        .WithParam(argumentsArray)
                        .WithParam("callException")).EndStatement();

            block.Write("}\r\n");

            block.Write("return").EndStatement();
        }

        private void ComposeParameterRead(ICodeBlockBuilder code, INamedReference readerField, ParameterInfo parameterInfo, INamedReference methodInfoField, INamedReference parameterInfoField, INamedReference contextParameter, INamedReference interceptorField)
        {
            code.InvokeGenericMethod(
                interceptorField,
                nameof(IControllerInterceptor.ObtainParameterValue),
                new Type[] { parameterInfo.ParameterType },
                inv =>
                    inv.WithParam("this")
                        .WithParam(methodInfoField)
                        .WithParam(parameterInfoField)
                        .WithParam(contextParameter)
                        .WithParam(
                            factory =>
                                factory.Write("() => ")
                                    .InvokeGenericMethod(
                                        readerField,
                                        nameof(IParameterReader.Read),
                                        new Type[] { parameterInfo.ParameterType },
                                        arguments => arguments.WithParam(parameterInfoField).WithParam(contextParameter))));
        }

        private Type GetResultWriterType(MethodInfo method)
        {
            if (Attribute.IsDefined(method, typeof(RawStringAttribute)))
            {
                return typeof(RawStringResultWriter);
            }
            else
            {
                return typeof(DefaultJsonSerializer);
            }
        }

        protected virtual Type GetParameterReaderType(ParameterInfo parameter)
        {
            return typeof(DefaultJsonSerializer);
        }
    }
}
