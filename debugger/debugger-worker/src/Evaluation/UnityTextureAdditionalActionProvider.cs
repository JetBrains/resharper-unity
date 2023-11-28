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
using Mono.Debugging.Backend.Values.ValueRoles;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Values;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.MetadataLite.API;
using Mono.Debugging.Soft;
using Mono.Debugging.TypeSystem;
using Newtonsoft.Json;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Evaluation
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    internal class UnityTextureAdditionalActionProvider : IAdditionalObjectActionProvider
    {
        private readonly Dictionary<ulong, IReifiedType<Value>> myReifiedTypesLocalCache = new();
        private readonly Dictionary<ulong, Value> myLoadedDllsLocalCache = new();
        private readonly ILogger myLogger;

        private const string GetPixelsMethodName = "GetPixelsInString";
        private static readonly Func<IMetadataMethodLite, bool> ourGetPixelsMethodFilter
            = m => m.Name == GetPixelsMethodName && m.Parameters.Length == 1;

        public UnityTextureAdditionalActionProvider(ILogger logger)
        {
            myLogger = logger;
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
            
            objectAction.EvaluateTexture.SetSync((lifetime, evaluationParameters) => DoTextureCalculations(lifetime, softValue, session, options, frame, evaluationParameters));
            return objectAction;
        }

        private UnityTextureAdditionalActionResult Error(string errorMessage)
        {
            myLogger.Error(errorMessage);
            return new UnityTextureAdditionalActionResult(errorMessage, null);
        }
        
        private  UnityTextureAdditionalActionResult DoTextureCalculations(Lifetime lifetime, Value softValue,
            SoftDebuggerSession session, IValueFetchOptions options, IStackFrame frame,
            UnityTextureAdditionalActionParams evaluationParameters)
        {
            var appDomainId = frame.GetAppDomainId();

            //Loading helpers dll
            try
            {
                if(!myLoadedDllsLocalCache.TryGetValue(appDomainId, out var loadedAssembly))
                {
                    loadedAssembly = session.DebuggingHelper.LoadAssembly(frame, evaluationParameters.HelperDllLocation);
                    if (loadedAssembly == null)
                        return Error(string.Format(Strings.UnityTextureDebuggingCannotLoadDllLabel, evaluationParameters.HelperDllLocation));
                    myLoadedDllsLocalCache.Add(appDomainId, loadedAssembly);
                }
            }
            catch (Exception e)
            {
                return Error(string.Format(Strings.UnityTextureDebuggingCannotLoadDllLabel, e));
            }
            
            //Loading required type
            IReifiedType<Value>? unityType;
            try
            {
                const string requiredType = "JetBrains.Debugger.Worker.Plugins.Unity.Presentation.Texture.UnityTextureAdapter";
                if(!myReifiedTypesLocalCache.TryGetValue(appDomainId, out unityType))
                {
                    unityType = session.TypeUniverse.GetReifiedType(frame, requiredType) as IReifiedType<Value>;
                    if (unityType == null)
                        return Error(string.Format(Strings.UnityTextureDubuggingCannotFindRequiredType, requiredType));
                    myReifiedTypesLocalCache.Add(appDomainId, unityType);
                }
            }
            catch (Exception e)
            {
                return Error(string.Format(Strings.UnityTextureDubuggingCannotFindRequiredType, e));
            }

            try
            {
                //Loading the texture
                if (unityType.CallStaticMethod(frame, options, ourGetPixelsMethodFilter, softValue) is not StringMirror stringValue)
                    return Error(string.Format(Strings.UnityTextureDubuggingCannotFindRequiredMethod, GetPixelsMethodName));

                var textureInfoJson = stringValue.Value;
                
                if (textureInfoJson.IsNullOrEmpty())
                    return Error(string.Format(Strings.UnityTextureDubuggingCannotGetTextureInfo, GetPixelsMethodName));

                //Parsing json
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
    }
}