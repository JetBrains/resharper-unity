using System;
using System.Linq;
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
using Mono.Debugging.Soft.MetadataLite.SoftMetadataLite;
using Mono.Debugging.Soft.Utils;
using Mono.Debugging.TypeSystem;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Evaluation
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class UnityDebugLogger : IBreakpointTraceHandler
    {
        private const string UnityEngineDebugTypeName = "UnityEngine.Debug";
        private const string UnityEngineDebugLogMethodName = "Log";

        protected ILogger Logger { get; }

        public UnityDebugLogger(IDebuggerSession session, IDebugSessionFrontend debugSessionFrontend, ILogger logger, IValueFactory<Value> factory)
        {
            mySession = session as SoftDebuggerSession;
            myIsUnityDebugSession =
                mySession != null && debugSessionFrontend is RiderDebuggerSessionFrontend riderDebuggerSessionFrontend
                                  && riderDebuggerSessionFrontend.SessionModel.StartInfo is UnityStartInfo
                                  && !mySession.IsIl2Cpp;//Disabled for il2cpp builds
            Logger = logger;
            myFactory = factory;
        }

        private readonly IValueFactory<Value> myFactory;
        private readonly SoftDebuggerSession? mySession;
        private readonly bool myIsUnityDebugSession;

        private static readonly Func<IMetadataMethodLite, bool> ourDebugLogMethodSelector
            = ml => ml.Name == UnityEngineDebugLogMethodName && ml.Parameters.Length == 1 && ml.Parameters[0].Type.IsObject();
        
        public bool Handle(BreakEvent be, IStackFrame activeFrame, string message)
        {
            if (!myIsUnityDebugSession)
                return false;

            var debugTypeNames = mySession.AppDomainsManager.ForceGetTypesForName(UnityEngineDebugTypeName, true, true)
                .ToArray();
            var debugType = debugTypeNames.FirstOrDefault();

            if (debugType == null)
            {
                Logger.Error($"Could not find {UnityEngineDebugTypeName} type in runtime.");
                return false;
            }

            // Somehow in case of UnityEngine it returns two same TypeMirrors with same ids and tokens
            // if (debugTypeNames.Length != 1)
            //     Logger.Error($"{UnityEngineDebugTypeName} types count == {debugTypeNames.Length}.");

            var evalOptions = mySession.Options.EvaluationOptions.AllowFullInvokes(true);

            try
            {
                var unityEngineDebugReifiedType =
                    (IReifiedType<Value>)mySession.TypeUniverse.GetReifiedType(activeFrame.GetAppDomainId(),
                        debugType.ToLite());
                var valueDebugMessage = myFactory.CreateString(activeFrame, evalOptions, message);

                var result = unityEngineDebugReifiedType.CallStaticMethod(activeFrame,
                    evalOptions.WithOverridden(x => x.EvaluationTimeout = 10000),
                    ourDebugLogMethodSelector,
                    valueDebugMessage);
                
                if (result == null)
                {
                    Logger.Error($"Failed to initialize {UnityEngineDebugTypeName}");
                    return false;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Failed to create {UnityEngineDebugTypeName}");
            }

            return true;
        }
    }
}