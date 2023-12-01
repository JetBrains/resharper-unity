using System;
using JetBrains.Debugger.Model.Plugins.Unity;
using JetBrains.Debugger.Worker.Plugins.Unity.Resources;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.Model.DebuggerWorker;
using JetBrains.Util;
using Mono.Debugger.Soft;
using Mono.Debugging.Autofac;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Values;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.MetadataLite.Services;
using Mono.Debugging.Soft;
using Mono.Debugging.Soft.CallStacks;
using Mono.Debugging.TypeSystem;
using Mono.Debugging.TypeSystem.KnownTypes;
using Newtonsoft.Json;
using Mono.Debugging.TypeSystem.KnownTypes.Predefined;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Evaluation
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    internal class UnityTextureAdditionalActionProvider : IAdditionalObjectActionProvider
    {
        private readonly ILogger myLogger;
        private readonly IKnownTypes<Value> myKnownTypes;

        private UnityDebuggingHelper? myHelper;

        public UnityTextureAdditionalActionProvider(ILogger logger, IValueFactory<Value> factory,
            IKnownTypes<Value> knownTypes)
        {
            myLogger = logger;
            myKnownTypes = knownTypes;
        }

        public ObjectAdditionalAction? CreateAction(IValueEntity valueEntity, IValueFetchOptions options,
            SoftDebuggerSession session, IStackFrame frame)
        {
            if (valueEntity is not IValue value)
                return null;

            var primaryRole = value.GetPrimaryRole(options);
            if (primaryRole is not IValueRole<Value> objectValueRole)
                return null;

            var objectAction = new UnityTextureAdditionalAction();
            var softValue = objectValueRole.ValueReference.GetValue(options);

            objectAction.EvaluateTexture.SetSync((_, evaluationParameters) =>
                DoTextureCalculations(softValue, options, frame, evaluationParameters));
            return objectAction;
        }

        private UnityTextureAdditionalActionResult Error(string errorMessage)
        {
            myLogger.Error(errorMessage);
            return new UnityTextureAdditionalActionResult(errorMessage, null);
        }

        private UnityTextureAdditionalActionResult DoTextureCalculations(Value softValue, IValueFetchOptions options,
            IStackFrame frame,
            UnityTextureAdditionalActionParams evaluationParameters)
        {
            var valueFetchOptions = options.WithOverridden(o => o.EvaluationTimeout = evaluationParameters.EvaluationTimeout);
            try
            {
                //Loading helpers dll
                if (myHelper == null || frame.GetAppDomainId() != myHelper.DomainTypes.AppDomainId)
                    myHelper = CreateUnityDebuggerHelper(frame, evaluationParameters, valueFetchOptions);
            }
            catch (Exception e)
            {
                // myHelper = null; //drop cached helper in case of esce 
                return Error(string.Format(Strings.UnityTextureDebuggingCannotLoadDllLabel, e));
            }

            try
            {
                //Loading the texture
                var value = myHelper.GetPixels(softValue).Call(frame, valueFetchOptions);
                var textureInfoJson = ((StringMirror)value).Value;

                var textureInfo = JsonConvert.DeserializeObject<UnityTextureInfo>(textureInfoJson);
                return textureInfo != null
                    ? new UnityTextureAdditionalActionResult(null, textureInfo)
                    : Error(string.Format(Strings.UnityTextureDubuggingCannotParseTextureInfo, textureInfoJson));
            }
            catch (Exception e)
            {
                return Error(string.Format(Strings.UnityTextureDubuggingCannotGetTextureInfo, e));
            }
        }

        private UnityDebuggingHelper CreateUnityDebuggerHelper(IStackFrame frame, UnityTextureAdditionalActionParams evaluationParameters, IValueFetchOptions options)
        {
            var domainId = frame.GetAppDomainId();
            var domainKnownTypes = (IDomainKnownTypes<Value>)myKnownTypes.ForDomain(domainId);
            
            var debuggingHelper = domainKnownTypes.DebuggingHelper(frame, options);
            var assembly = debuggingHelper.LoadAssemblyFromLocation(evaluationParameters.HelperDllLocation).Call(frame, options);

            // force loading of the unity helper assembly
            debuggingHelper
                .GetTypeByAssemblyAndTypeName(UnityDebuggingHelper.AssemblyName, UnityDebuggingHelper.RequiredType)
                .Call(frame, options);

            var unityAssemblyReifiedType = domainKnownTypes.KnownTypes.TypeUniverse.GetReifiedType(frame, UnityDebuggingHelper.RequiredTypeWithAssembly);
            if (unityAssemblyReifiedType == null)
            {
                myLogger.Warn("We haven't got a unity helper assembly load event, trying to force it");
                
                frame.GetSoftAppDomain().GetAssemblies(forceResetCache: true);
                unityAssemblyReifiedType = domainKnownTypes.KnownTypes.TypeUniverse.GetReifiedType(frame, UnityDebuggingHelper.RequiredTypeWithAssembly);
                if (unityAssemblyReifiedType == null)
                    throw new Exception("Unable to call a unity helper methods as we don't have metadata of this assembly");
            }

            return new UnityDebuggingHelper((IReifiedType<Value>)unityAssemblyReifiedType, domainKnownTypes);
        }
    }
}