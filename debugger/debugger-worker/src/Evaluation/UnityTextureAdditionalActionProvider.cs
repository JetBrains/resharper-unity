using System;
using JetBrains.Application.Settings;
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
using Mono.Debugging.TypeSystem.KnownTypes;
using Newtonsoft.Json;

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
            UnityDebuggingHelper helper; 
            var valueFetchOptions = options.WithOverridden(o => o.EvaluationTimeout = evaluationParameters.EvaluationTimeout);
            try
            {
                //Loading helpers dll
                helper = GetUnityDebuggerHelper(frame, evaluationParameters, valueFetchOptions);
            }
            catch (Exception e)
            {
                return Error(string.Format(Strings.UnityTextureDebuggingCannotLoadDllLabel, e));
            }

            try
            {
                //Loading the texture
                var value = helper.GetPixels(softValue).Call(frame, valueFetchOptions);
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

        private UnityDebuggingHelper GetUnityDebuggerHelper(IStackFrame frame,
            UnityTextureAdditionalActionParams evaluationParameters, IValueFetchOptions options)
        {
            if (myHelper != null)
                return myHelper;

            myKnownTypes.LoadAssemblyWithGetTypeCall(frame, options, evaluationParameters.HelperDllLocation,
                UnityDebuggingHelper.RequiredType);
            return myHelper = new UnityDebuggingHelper(myKnownTypes.ForDomain(frame.GetAppDomainId()));
        }
    }
}