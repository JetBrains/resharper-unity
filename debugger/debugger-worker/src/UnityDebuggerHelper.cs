using System;
using JetBrains.Debugger.Model.Plugins.Unity;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Soft.CallStacks;
using Mono.Debugging.TypeSystem;
using Mono.Debugging.TypeSystem.KnownTypes;

namespace JetBrains.Debugger.Worker.Plugins.Unity
{
    public abstract class UnityDebuggerHelper<TValue> : KnownTypeBase<TValue> where TValue : class
    {
        protected UnityDebuggerHelper(IReifiedType<TValue> reifiedType, IDomainKnownTypes<TValue> domainTypes) : base(
            reifiedType, domainTypes)
        {
        }

        protected delegate T FactoryDelegate<out T>(IReifiedType<TValue> reifiedType, IDomainKnownTypes<TValue> domainTypes) where T : UnityDebuggerHelper<TValue>;

        protected static T CreateUnityDebuggerHelper<T>(IStackFrame frame, IValueFetchOptions options,
            IKnownTypes<TValue> knownTypes, UnityBundleInfo assemblyBundleInfo, string requiredType, FactoryDelegate<T> factory)
            where T : UnityDebuggerHelper<TValue>
        {
            var domainId = frame.GetAppDomainId();
            var domainKnownTypes = knownTypes.ForDomain(domainId);
            
            var debuggingHelper = domainKnownTypes.DebuggingHelper(frame, options);
            var assembly = debuggingHelper.LoadAssemblyFromLocation(assemblyBundleInfo.AbsolutePath).Call(frame, options);

            // force loading of the unity helper assembly
            debuggingHelper
                .GetTypeByAssemblyAndTypeName(assemblyBundleInfo.Id, requiredType)
                .Call(frame, options);

            var requiredTypeWithAssembly = $"{requiredType}, {assemblyBundleInfo.Id}";
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

            return factory((IReifiedType<TValue>)unityAssemblyReifiedType, domainKnownTypes);
        }
        
        public static string GetAssemblyName(string assemblyBaseName, bool isDotNetCore)
        {
            var assemblyName = assemblyBaseName;
            if (isDotNetCore) assemblyName += ".DotNetCore";
            return assemblyName;
        }
    }
}