using System;
using System.Collections.Generic;
using JetBrains.Debugger.Model.Plugins.Unity;
using JetBrains.Debugger.Worker.Plugins.Unity.Resources;
using JetBrains.Debugger.Worker.Plugins.Unity.SessionStartup;
using JetBrains.Lifetimes;
using JetBrains.Rider.Model.DebuggerWorker;
using JetBrains.Util;
using Mono.Debugger.Soft;
using Mono.Debugging.Autofac;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Values;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Soft;
using Mono.Debugging.TypeSystem.KnownTypes;
using Mono.Debugging.Win32;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Evaluation
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    internal class MonoUnityTextureAdditionalPropertiesProvider : UnityTextureAdditionalPropertiesProvider<Value>
    {
        public MonoUnityTextureAdditionalPropertiesProvider(ILogger logger, SoftDebuggerSession debuggerSession, IKnownTypes<Value> knownTypes, ISessionCreationInfo creationInfo, IUnityOptions unityOptions) : base(logger, debuggerSession, knownTypes, creationInfo, unityOptions)
        {
        }
    }

    [DebuggerSessionComponent(typeof(CorDebuggerType))]
    internal class CorUnityTextureAdditionalPropertiesProvider : UnityTextureAdditionalPropertiesProvider<ICorValue>
    {
        public CorUnityTextureAdditionalPropertiesProvider(ILogger logger, CorDebuggerSession debuggerSession, IKnownTypes<ICorValue> knownTypes, ISessionCreationInfo creationInfo, IUnityOptions unityOptions) : base(logger, debuggerSession, knownTypes, creationInfo, unityOptions)
        {
        }
    }

    internal class UnityTextureAdditionalPropertiesProvider<TValue> : IAdditionalObjectPropertiesProvider where TValue : class
    {
        private readonly ILogger myLogger;
        private readonly IKnownTypes<TValue> myKnownTypes;

        private readonly IUnityOptions myUnityOptions;
        private UnityTextureDebuggerHelper<TValue>? myHelper;
        private readonly UnityBundleInfo? myAssemblyBundleInfo;

        protected UnityTextureAdditionalPropertiesProvider(ILogger logger, DebuggerSession<TValue> debuggerSession,
            IKnownTypes<TValue> knownTypes, ISessionCreationInfo creationInfo, IUnityOptions unityOptions)
        {
            myLogger = logger;
            myKnownTypes = knownTypes;
            myUnityOptions = unityOptions;

            if (creationInfo.StartInfo is UnityStartInfo unityStartInfo)
            {
                var assemblyName = UnityTextureDebuggerHelper<TValue>.GetAssemblyName(debuggerSession.DebugeeRuntime.IsDotNetCore());
                myAssemblyBundleInfo = unityStartInfo.GetBundleInfo(assemblyName, logger);
            }
        }

        public AdditionalObjectPropertiesData? Create(PausedContext pausedContext, IValueEntity valueEntity,
            IValueFetchOptions options)
        {
            if(!myUnityOptions.ExtensionsEnabled || myAssemblyBundleInfo == null)
                return null;
            
            options = options.AllowFullInvokes();
            if (valueEntity is not IValue value)
                return null;

            var primaryRole = value.GetPrimaryRole(options);
            if (primaryRole is not IValueRole<TValue> objectValueRole)
                return null;

            var objectAction = new UnityTexturePropertiesData();
            var softValue = objectValueRole.ValueReference.GetValue(options);

            objectAction.EvaluateTexture.SetRdTask((rdCallLifetime, evaluationParameters) =>
            {
                return pausedContext.EnqueueTask(rdCallLifetime, nameof(objectAction.EvaluateTexture),
                    lifetime => pausedContext.WithFrame(evaluationParameters.FrameId, 
                        frame => DoTextureCalculations(softValue, options, frame, evaluationParameters, lifetime)));
            });
            return objectAction;
        }

        private UnityTextureAdditionalActionResult Error(string errorMessage)
        {
            myLogger.Error(errorMessage);
            return new UnityTextureAdditionalActionResult(errorMessage, null, false);
        }

        private UnityTextureAdditionalActionResult DoTextureCalculations(TValue softValue, IValueFetchOptions options,
            IStackFrame frame,
            UnityTextureAdditionalActionParams evaluationParameters, Lifetime lifetime)
        {
            if (lifetime.IsNotAlive)
                return new UnityTextureAdditionalActionResult(null, null, true);

            var valueFetchOptions = options
                .AllowFullInvokes()
                .WithOverridden(o => o.EvaluationTimeout = evaluationParameters.EvaluationTimeout);
            try
            {
                //Loading helpers dll
                if (myHelper == null || frame.GetAppDomainId() != myHelper.DomainTypes.AppDomainId)
                    myHelper = UnityTextureDebuggerHelper<TValue>.CreateHelper(frame, valueFetchOptions, myKnownTypes,
                        myAssemblyBundleInfo);
            }
            catch (Exception e)
            {
                return Error(string.Format(Strings.UnityTextureDebuggingCannotLoadDllLabel, e));
            }

            if (lifetime.IsNotAlive)
                return new UnityTextureAdditionalActionResult(null, null, true);

            try
            {
                //Loading the texture
                var value = myHelper.GetPixels(softValue).Call(frame, valueFetchOptions);

                var simpleValueReference =
                    new SimpleValueReference<TValue>(value, frame, myKnownTypes.RoleFactory);

                if (simpleValueReference.GetPrimaryRole(valueFetchOptions) is not IObjectValueRole<TValue> primaryRole)
                    return Error(Strings.UnityTextureDubuggingCannotParseTextureInfo);

                var fieldReferences = primaryRole.GetInstanceFieldReferences();

                return GetTextureInfo(fieldReferences, valueFetchOptions, lifetime);
            }
            catch (Exception e)
            {
                return Error(string.Format(Strings.UnityTextureDubuggingCannotGetTextureInfo, e));
            }
        }

        private UnityTextureAdditionalActionResult GetTextureInfo(
            IEnumerable<IFieldValueReference<TValue>> heightReferences,
            IValueFetchOptions valueFetchOptions, Lifetime lifetime)
        {
            var width = -1;
            var height = -1;
            List<int>? pixels = null;
            var originalWidth = -1;
            var originalHeight = -1;
            string? graphicsTextureFormat = null;
            string? textureName = null;
            var hasAlphaChannel = false;

            var hasError = false;
            foreach (var valueReference in heightReferences)
            {
                if (lifetime.IsNotAlive)
                    return new UnityTextureAdditionalActionResult(null, null, true);

                switch (valueReference.DefaultName)
                {
                    case nameof(UnityTextureInfo.Height):
                        height = (int)(valueReference.AsPrimitiveSafe(valueFetchOptions)?.GetPrimitive() ?? height);
                        break;
                    case nameof(UnityTextureInfo.Width):
                        width = (int)(valueReference.AsPrimitiveSafe(valueFetchOptions)?.GetPrimitive() ?? width);
                        break;
                    case nameof(UnityTextureInfo.OriginalHeight):
                        originalHeight = (int)(valueReference.AsPrimitiveSafe(valueFetchOptions)?.GetPrimitive() ??
                                               originalHeight);
                        break;
                    case nameof(UnityTextureInfo.OriginalWidth):
                        originalWidth = (int)(valueReference.AsPrimitiveSafe(valueFetchOptions)?.GetPrimitive() ??
                                              originalWidth);
                        break;
                    case nameof(UnityTextureInfo.HasAlphaChannel):
                        hasAlphaChannel = (bool)(valueReference.AsPrimitiveSafe(valueFetchOptions)?.GetPrimitive() ??
                                                 hasAlphaChannel);
                        break;
                    case nameof(UnityTextureInfo.Pixels):
                        var arrayValueRole = valueReference.AsArray(valueFetchOptions);
                        pixels = new(arrayValueRole.ReadAllElements<TValue, int>());
                        break;
                    case nameof(UnityTextureInfo.TextureName):
                        textureName = valueReference.AsStringSafe(valueFetchOptions)?.GetString();
                        break;
                    case nameof(UnityTextureInfo.GraphicsTextureFormat):
                        graphicsTextureFormat = valueReference.AsStringSafe(valueFetchOptions)?.GetString();
                        break;
                }
            }


            if (hasError
                || width < 0 || height < 0 //value validation
                || pixels == null
                || originalHeight < 0 || originalWidth < 0
                || graphicsTextureFormat == null || textureName == null)
                return Error(Strings.UnityTextureDubuggingCannotParseTextureInfo);

            return new UnityTextureAdditionalActionResult(null, new UnityTextureInfo(width, height, pixels,
                originalWidth, originalHeight, graphicsTextureFormat,
                textureName, hasAlphaChannel), false);
        }
    }
}