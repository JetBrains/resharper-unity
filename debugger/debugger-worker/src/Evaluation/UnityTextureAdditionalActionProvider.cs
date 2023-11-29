using System;
using System.Collections.Generic;
using DebuggingHelp;
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
using Mono.Debugging.MetadataLite.API;
using Mono.Debugging.MetadataLite.API.Selectors;
using Mono.Debugging.MetadataLite.Services;
using Mono.Debugging.Soft;
using Mono.Debugging.TypeSystem;
using Mono.Debugging.TypeSystem.KnownTypes;
using Mono.Debugging.TypeSystem.KnownTypes.Predefined;
using Newtonsoft.Json;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Evaluation
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    internal class UnityTextureAdditionalActionProvider : IAdditionalObjectActionProvider
    {
        private readonly Dictionary<ulong, IReifiedType<Value>> myReifiedTypesLocalCache = new();
        private readonly Dictionary<ulong, Value> myLoadedDllsLocalCache = new();
        private readonly ILogger myLogger;
        private readonly IValueFactory<Value> myFactory;
        private readonly IKnownTypes<Value> myKnownTypes;

        private const string GetPixelsMethodName = "GetPixelsInString";
        private static readonly Func<IMetadataMethodLite, bool> ourGetPixelsMethodFilter
            = m => m.Name == GetPixelsMethodName && m.Parameters.Length == 1;

        private const string RequiredType = "JetBrains.Debugger.Worker.Plugins.Unity.Presentation.Texture.UnityTextureAdapter";
        
        public UnityTextureAdditionalActionProvider(ILogger logger, IValueFactory<Value> factory, IKnownTypes<Value> knownTypes)
        {
            myLogger = logger;
            myFactory = factory;
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
                // if(!myLoadedDllsLocalCache.TryGetValue(appDomainId, out var loadedAssembly))
                {
                 
                    // var evaluationRequest = "System.Reflection.Assembly.LoadFile(@\"${bundledFile.absolutePath}\")"
                    // const string reflectionAssembly = "System.Reflection.Assembly";
                    // var reflectionType = session.TypeUniverse.GetReifiedType(frame, reflectionAssembly) as IReifiedType<Value>;
                    //
                    // if (reflectionType == null)
                    //     return Error(string.Format(Strings.UnityTextureDebuggingCannotLoadDllLabel, evaluationParameters.HelperDllLocation));
                    //
                    // var evalOptions = session.Options.EvaluationOptions.AllowFullInvokes().WithOverridden(o =>
                    // {
                    //     o.EvaluationTimeout = 10000;
                    // });
                    //
                    // var assemblyPath = myFactory.CreateString(frame, evalOptions, evaluationParameters.HelperDllLocation);
                    // var value = reflectionType.CallStaticMethod(frame, options, m => m.Name == "LoadFile" && m.Parameters.Length == 1, assemblyPath);

                    var helper = GetUnityDebuggerHelper(frame, evaluationParameters, options);
                    var value = helper.GetPixels(softValue).Call(frame, options);
                    var textureInfoJson = ((StringMirror)value).Value;
                    
                    var textureInfo = JsonConvert.DeserializeObject<UnityTextureInfo>(textureInfoJson);
                    return textureInfo != null
                        ? new UnityTextureAdditionalActionResult(null, textureInfo)
                        : Error(string.Format(Strings.UnityTextureDubuggingCannotParseTextureInfo, textureInfoJson));
                    // var path = FileSystemPath.TryParse(evaluationParameters.HelperDllLocation);
                    // var bytes = path.ReadAllBytes();
                    // var assemblyValue = myKnownTypes.ForDomain(appDomainId).Assembly.AssemblyLoad(bytes).Call(frame, options);

                    // if(assemblyValue == null)
                    // return Error(string.Format(Strings.UnityTextureDebuggingCannotLoadDllLabel, evaluationParameters.HelperDllLocation));

                    // var helperAssembly =  new SimpleValueReference<Value>(assemblyValue, frame, myKnownTypes.RoleFactory)
                    // .AsObjectSafe(options);
                    // if(helperAssembly == null)
                    // return Error(string.Format(Strings.UnityTextureDebuggingCannotLoadDllLabel, evaluationParameters.HelperDllLocation));


                    // var assemblyGetType = helperAssembly.ReifiedType.MetadataType.LookupInstanceMethod(MethodSelectors.AssemblyGetType__Name);
                    // var helperTypeNameValue = myKnownTypes.ValueFactory.CreateString(frame, options, RequiredType);
                    // var getTypeValue = helperAssembly.CallInstanceMethod(assemblyGetType, helperTypeNameValue);
                    // if(getTypeValue == null)
                    // return Error(string.Format(Strings.UnityTextureDebuggingCannotLoadDllLabel, evaluationParameters.HelperDllLocation));


                    // var textureDebugHelperType =  new SimpleValueReference<Value>(getTypeValue, frame, myKnownTypes.RoleFactory)
                    // .AsObjectSafe(options);


                    // if(textureDebugHelperType == null)
                    // return Error(string.Format(Strings.UnityTextureDebuggingCannotLoadDllLabel, evaluationParameters.HelperDllLocation));

                    // var valueReference = textureDebugHelperType.ValueReference;
                    // var getPixelsMethod = textureDebugHelperType.ReifiedType.MetadataType.LookupStaticMethod(ourGetPixelsMethodFilter);

                    // myKnownTypes.ForDomain(appDomainId)

                    // var lookupInstanceMethod = value.ReifiedType.MetadataType.LookupInstanceMethod(ourGetPixelsMethodFilter);
                    // var callInstanceMethod = value.CallInstanceMethod(lookupInstanceMethod, softValue);

                    // if (session.DebuggingHelper.TryLoadAssembly(frame, evaluationParameters.HelperDllLocation))
                    // return Error(string.Format(Strings.UnityTextureDebuggingCannotLoadDllLabel, evaluationParameters.HelperDllLocation));
                    // myLoadedDllsLocalCache.Add(appDomainId, loadedAssembly);
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
                // if(!myReifiedTypesLocalCache.TryGetValue(appDomainId, out unityType))
                {
                    var reifiedType = session.TypeUniverse.GetReifiedType(frame, UnityDebuggingHelper.RequiredType);
                    unityType = reifiedType as IReifiedType<Value>;
                    if (unityType == null)
                        return Error(string.Format(Strings.UnityTextureDubuggingCannotFindRequiredType, UnityDebuggingHelper.RequiredType));
                    // myReifiedTypesLocalCache.Add(appDomainId, unityType);
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
        
        private UnityDebuggingHelper GetUnityDebuggerHelper(IStackFrame frame, UnityTextureAdditionalActionParams evaluationParameters, IValueFetchOptions options)
        {
            if (myHelper != null)
                return myHelper;
            
            myKnownTypes.LoadAssemblyWithGetTypeCall(frame, options, evaluationParameters.HelperDllLocation, UnityDebuggingHelper.RequiredType);
            return myHelper = new UnityDebuggingHelper(myKnownTypes.ForDomain(frame.GetAppDomainId()));
        }
    }
}