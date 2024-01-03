using System;
using System.Collections.Generic;
using JetBrains.Debugger.Model.Plugins.Unity;
using JetBrains.Debugger.Worker.Plugins.Unity.Resources;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.Model.DebuggerWorker;
using JetBrains.Util;
using Mono.Debugger.Soft;
using Mono.Debugging.Autofac;
using Mono.Debugging.Backend.Values.ValueReferences;
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Values;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.MetadataLite.Services;
using Mono.Debugging.Soft;
using Mono.Debugging.Soft.CallStacks;
using Mono.Debugging.Soft.Values.ValueRoles;
using Mono.Debugging.TypeSystem;
using Mono.Debugging.TypeSystem.KnownTypes;

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

        public ObjectAdditionalAction? CreateAction(IValueEntity valueEntity, IValueFetchOptions options, IStackFrame frame)
        {
            if (valueEntity is not IValue value)
                return null;

            var primaryRole = value.GetPrimaryRole(options);
            if (primaryRole is not IValueRole<Value> objectValueRole)
                return null;

            var objectAction = new UnityTextureAdditionalAction();
            var softValue = objectValueRole.ValueReference.GetValue(options);

            objectAction.EvaluateTexture.SetSync((lifetime, evaluationParameters) =>
                DoTextureCalculations(softValue, options, frame, evaluationParameters, lifetime));
            return objectAction;
        }

        private UnityTextureAdditionalActionResult Error(string errorMessage)
        {
            myLogger.Error(errorMessage);
            return new UnityTextureAdditionalActionResult(errorMessage, null, false);
        }

        private UnityTextureAdditionalActionResult DoTextureCalculations(Value softValue, IValueFetchOptions options,
            IStackFrame frame,
            UnityTextureAdditionalActionParams evaluationParameters, Lifetime lifetime)
        {
            if (lifetime.IsNotAlive)
                return new UnityTextureAdditionalActionResult(null, null, true);
            
            var valueFetchOptions = options.WithOverridden(o => o.EvaluationTimeout = evaluationParameters.EvaluationTimeout);
            try
            {
                //Loading helpers dll
                if (myHelper == null || frame.GetAppDomainId() != myHelper.DomainTypes.AppDomainId)
                    myHelper = CreateUnityDebuggerHelper(frame, evaluationParameters, valueFetchOptions);
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

                if(value is not ObjectMirror objectMirror)
                    return Error(Strings.UnityTextureDubuggingCannotParseTextureInfo);
                
                var simpleValueReference = new SimpleValueReference<Value>(objectMirror, frame, myKnownTypes.RoleFactory);

                if (simpleValueReference.GetPrimaryRole(valueFetchOptions) is not SoftObjectValueRole primaryRole)
                    return Error(Strings.UnityTextureDubuggingCannotParseTextureInfo);
                
                var fieldReferences = primaryRole.GetInstanceFieldReferences();

                return GetTextureInfo(fieldReferences, valueFetchOptions, lifetime);
            }
            catch (Exception e)
            {
                return Error(string.Format(Strings.UnityTextureDubuggingCannotGetTextureInfo, e));
            }
        }

        private UnityTextureAdditionalActionResult GetTextureInfo(IEnumerable<IFieldValueReference<Value>> heightReferences,
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
                if(lifetime.IsNotAlive)
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
                        originalHeight = (int)(valueReference.AsPrimitiveSafe(valueFetchOptions)?.GetPrimitive() ?? originalHeight);
                        break;
                    case nameof(UnityTextureInfo.OriginalWidth):
                        originalWidth = (int)(valueReference.AsPrimitiveSafe(valueFetchOptions)?.GetPrimitive() ?? originalWidth);
                        break;
                    case nameof(UnityTextureInfo.HasAlphaChannel):
                        hasAlphaChannel = (bool)(valueReference.AsPrimitiveSafe(valueFetchOptions)?.GetPrimitive() ?? hasAlphaChannel);
                        break;
                    case nameof(UnityTextureInfo.Pixels):
                        var arrayValueRole = valueReference.AsArray(valueFetchOptions);
                        var length = arrayValueRole.Dimensions[0];

                        var values = (valueReference.GetValue(valueFetchOptions) as ArrayMirror)?.GetValues(0, length);
                        if (values == null)
                        {
                            hasError = true;
                            break;
                        }
                        
                        pixels = new List<int>(length);
                        for (int i = 0; i < length; i++)
                        {
                            pixels.Add((int)((PrimitiveValue)values[i]).Value);
                        }
                        
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
                || width < 0 || height < 0  //value validation
                || pixels == null
                || originalHeight < 0 || originalWidth < 0
                || graphicsTextureFormat == null || textureName == null)
                return Error(Strings.UnityTextureDubuggingCannotParseTextureInfo);;

            return new UnityTextureAdditionalActionResult(null,  new UnityTextureInfo(width, height, pixels, originalWidth, originalHeight, graphicsTextureFormat,
                textureName, hasAlphaChannel), false);
        }
        
        

        private UnityDebuggingHelper CreateUnityDebuggerHelper(IStackFrame frame, UnityTextureAdditionalActionParams evaluationParameters, IValueFetchOptions options)
        {
            var domainId = frame.GetAppDomainId();
            var domainKnownTypes = myKnownTypes.ForDomain(domainId);
            
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