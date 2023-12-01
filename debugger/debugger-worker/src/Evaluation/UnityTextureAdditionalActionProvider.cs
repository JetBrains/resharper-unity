using System;
using System.Collections.Generic;
using JetBrains.Debugger.Model.Plugins.Unity;
using JetBrains.Debugger.Worker.Plugins.Unity.Resources;
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
                return Error(string.Format(Strings.UnityTextureDebuggingCannotLoadDllLabel, e));
            }

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
                var unityTextureInfo = GetTextureInfo(fieldReferences, valueFetchOptions);

                return unityTextureInfo != null
                    ? new UnityTextureAdditionalActionResult(string.Empty, unityTextureInfo)
                    : Error(Strings.UnityTextureDubuggingCannotParseTextureInfo);
            }
            catch (Exception e)
            {
                return Error(string.Format(Strings.UnityTextureDubuggingCannotGetTextureInfo, e));
            }
        }

        private static UnityTextureInfo? GetTextureInfo(IEnumerable<IFieldValueReference<Value>> heightReferences, IValueFetchOptions valueFetchOptions)
        {
            T GetPrimitiveValue<T>(IValueReference<Value> valueReference, ref bool hasError)
                where T : struct
            {
                if (valueReference.GetValue(valueFetchOptions) is PrimitiveValue primitiveValue) 
                    return (T)primitiveValue.Value;
                
                hasError = true;
                return default;
            }

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
                switch (valueReference.DefaultName)
                {
                    case nameof(UnityTextureInfo.Height):
                        height = GetPrimitiveValue<int>(valueReference, ref hasError);    
                        break;
                    case nameof(UnityTextureInfo.Width):
                        width = GetPrimitiveValue<int>(valueReference, ref hasError);
                        break;
                    case nameof(UnityTextureInfo.OriginalHeight):
                        originalHeight = GetPrimitiveValue<int>(valueReference, ref hasError);
                        break;
                    case nameof(UnityTextureInfo.OriginalWidth):
                        originalWidth = GetPrimitiveValue<int>(valueReference, ref hasError);
                        break;
                    case nameof(UnityTextureInfo.HasAlphaChannel):
                        hasAlphaChannel = GetPrimitiveValue<bool>(valueReference, ref hasError);
                        break;
                    case nameof(UnityTextureInfo.Pixels):
                        if (valueReference.GetValue(valueFetchOptions) is ArrayMirror arrayMirror)
                        {
                            var length = arrayMirror.GetLength(0);
                            pixels = new List<int>(length);
                            var values = arrayMirror.GetValues(0, length);
                            foreach (var value in values)
                            {
                                if (value is PrimitiveValue primitiveValue)
                                    pixels.Add((int)primitiveValue.Value);
                                else
                                {
                                    hasError = true;
                                    break;
                                }
                            }
                        }
                        else
                            hasError = true;
                        break;
                    case nameof(UnityTextureInfo.TextureName):
                        if (valueReference.GetValue(valueFetchOptions) is StringMirror textureNameStringMirror)
                            textureName = textureNameStringMirror.Value;
                        else
                            hasError = true;
                        break;
                    case nameof(UnityTextureInfo.GraphicsTextureFormat):
                        if (valueReference.GetValue(valueFetchOptions) is StringMirror graphicsTextureFormatStringMirror)
                            graphicsTextureFormat = graphicsTextureFormatStringMirror.Value;
                        else
                            hasError = true;
                        break;
                }
            }


            if (hasError
                || width < 0 || height < 0  //value validation
                || pixels == null
                || originalHeight < 0 || originalWidth < 0
                || graphicsTextureFormat == null || textureName == null)
                return null;

            return new UnityTextureInfo(width, height, pixels, originalWidth, originalHeight, graphicsTextureFormat,
                textureName, hasAlphaChannel);
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