using System;
using System.Collections.Generic;
using JetBrains.Debugger.Model.Plugins.Unity;
using JetBrains.Util;
using Mono.Debugger.Soft;
using Mono.Debugging.Autofac;
using Mono.Debugging.Client;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.MetadataLite.API;
using Mono.Debugging.MetadataLite.Services;
using Mono.Debugging.Soft;
using Mono.Debugging.TypeSystem;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Evaluation
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class UnityDebugLogger : IBreakpointTraceHandler
    {
        private const string UnityEngineDebugTypeName = "UnityEngine.Debug";
        private const string UnityEngineDebugLogMethodName = "Log";


        public UnityDebugLogger(IDebuggerSession session, IDebugSessionFrontend debugSessionFrontend, ILogger logger, IValueFactory<Value> factory)
        {
            mySession = session as SoftDebuggerSession;
            myIsUnityDebugSession =
                mySession != null && debugSessionFrontend is RiderDebuggerSessionFrontend riderDebuggerSessionFrontend
                                  && riderDebuggerSessionFrontend.SessionModel.StartInfo is UnityStartInfo;
                                  
            myLogger = logger;
            myFactory = factory;
        }

        private readonly IValueFactory<Value> myFactory;
        private readonly SoftDebuggerSession? mySession;
        private readonly bool myIsUnityDebugSession;
        private readonly ILogger myLogger;
        private readonly Dictionary<ulong, IReifiedType<Value>> myReifiedTypesLocalCache = new();

        private static readonly Func<IMetadataMethodLite, bool> ourDebugLogMethodFilter
            = ml => ml.Name == UnityEngineDebugLogMethodName && ml.Parameters.Length == 1 && ml.Parameters[0].Type.IsObject();

        public bool Handle(BreakEvent be, IStackFrame activeFrame, string message)
        {
            if (!myIsUnityDebugSession || mySession == null || mySession.IsIl2Cpp) //Disabled for il2cpp builds
                return false;
            
            var debugType = mySession.TypeUniverse.GetTypeByAssemblyQualifiedName(activeFrame, UnityEngineDebugTypeName);

            if (debugType == null)
            {
                myLogger.Error($"Could not find {UnityEngineDebugTypeName} type in runtime.");
                return false;
            }

            var evalOptions = mySession.Options.EvaluationOptions.AllowFullInvokes();

            try
            {
                var appDomainId = activeFrame.GetAppDomainId();
                if (!myReifiedTypesLocalCache.TryGetValue(appDomainId, out var unityEngineDebugReifiedType))
                {
                    unityEngineDebugReifiedType = (IReifiedType<Value>)mySession.TypeUniverse.GetReifiedType(appDomainId, debugType);
                    myReifiedTypesLocalCache.Add(appDomainId, unityEngineDebugReifiedType);
                }
                
                var valueDebugMessage = myFactory.CreateString(activeFrame, evalOptions, message);

                var result = unityEngineDebugReifiedType.CallStaticMethod(activeFrame, 
                    evalOptions,
                    ourDebugLogMethodFilter,
                    valueDebugMessage);
                
                if (result == null)
                {
                    myLogger.Error($"Failed to initialize {UnityEngineDebugTypeName}");
                    return false;
                }
            }
            catch (Exception e)
            {
                myLogger.Error(e, $"Failed to create {UnityEngineDebugTypeName}");
            }

            return true;
        }
    }
}