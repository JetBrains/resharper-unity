using System;
using Mono.Debugger.Soft;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Soft.CallStacks;
using Mono.Debugging.TypeSystem;
using Mono.Debugging.TypeSystem.KnownTypes;

namespace JetBrains.Debugger.Worker.Plugins.Unity
{
    public abstract class UnityDebuggerHelper : KnownTypeBase<Value>
    {
        protected UnityDebuggerHelper(IReifiedType<Value> reifiedType, IDomainKnownTypes<Value> domainTypes) : base(
            reifiedType, domainTypes)
        {
        }

        protected delegate T FactoryDelegate<out T>(IReifiedType<Value> reifiedType, IDomainKnownTypes<Value> domainTypes) where T : UnityDebuggerHelper;

        protected static T CreateUnityDebuggerHelper<T>(IStackFrame frame, IValueFetchOptions options,
            IKnownTypes<Value> knownTypes, string assemblyLocation, string assemblyName, string requiredType, FactoryDelegate<T> factory)
            where T : UnityDebuggerHelper
        {
            var domainId = frame.GetAppDomainId();
            var domainKnownTypes = knownTypes.ForDomain(domainId);

            var debuggingHelper = domainKnownTypes.DebuggingHelper(frame, options);
            var assembly = debuggingHelper.LoadAssemblyFromLocation(assemblyLocation).Call(frame, options);

            // force loading of the unity helper assembly
            debuggingHelper
                .GetTypeByAssemblyAndTypeName(assemblyName, requiredType)
                .Call(frame, options);


            var requiredTypeWithAssembly = $"{requiredType}, {assemblyName}";
            var unityAssemblyReifiedType =
                domainKnownTypes.KnownTypes.TypeUniverse.GetReifiedType(frame, requiredTypeWithAssembly);
            if (unityAssemblyReifiedType == null)
            {   
                // myLogger.Warn("We haven't got a unity helper assembly load event, trying to force it");

                frame.GetSoftAppDomain().GetAssemblies(forceResetCache: true);
                unityAssemblyReifiedType =
                    domainKnownTypes.KnownTypes.TypeUniverse.GetReifiedType(frame, requiredTypeWithAssembly);
                if (unityAssemblyReifiedType == null)
                    throw new Exception(
                        "Unable to call a unity helper methods as we don't have metadata of this assembly");
            }

            return factory((IReifiedType<Value>)unityAssemblyReifiedType, domainKnownTypes);
        }
    }
}