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
    [Flags]
    internal enum BreakpointTraceOutput
    {
        UnityOutput = 1 << 0,
        DebugConsole = 1 << 1,
        Both = UnityOutput | DebugConsole
    }

    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class UnityDebugLogger : IBreakpointTraceHandler
    {
        private const string UnityEngineDebugTypeName = "UnityEngine.Debug";
        private const string UnityEngineDebugLogMethodName = "Log";


        public UnityDebugLogger(IUnityOptions unityOptions,
            ISessionCreationInfo creationInfo,
            IDebuggerSessionInternal debuggerSession,
            ILogger logger,
            IValueFactory<Value> factory)
        {
            myUnityOptions = unityOptions;
            myIsUnityDebugSession = unityOptions.ExtensionsEnabled && creationInfo.StartInfo is UnityStartInfo;

            myDebuggerSession = debuggerSession;
            myLogger = logger;
            myFactory = factory;
        }

        private readonly IValueFactory<Value> myFactory;
        private readonly bool myIsUnityDebugSession;
        private readonly IUnityOptions myUnityOptions;
        private readonly IDebuggerSessionInternal myDebuggerSession;
        private readonly ILogger myLogger;
        private readonly Dictionary<ulong, IReifiedType<Value>> myReifiedTypesLocalCache = new();

        private BreakpointTraceOutput BreakpointTraceOutputSettings =>
            (BreakpointTraceOutput)myUnityOptions.BreakpointTraceOutput;

        private static readonly Func<IMetadataMethodLite, bool> ourDebugLogMethodFilter
            = ml => ml.Name == UnityEngineDebugLogMethodName && ml.Parameters.Length == 1 &&
                    ml.Parameters[0].Type.IsObject();

        public bool DoHandle(BreakEvent be, IStackFrame activeFrame, IDebuggerSession session, string message)
        {
            if (!myIsUnityDebugSession || session.IsIl2Cpp) //Disabled for il2cpp builds
                return false;

            var debugType = session.TypeUniverse.GetTypeByAssemblyQualifiedName(activeFrame, UnityEngineDebugTypeName);

            if (debugType == null)
            {
                myLogger.Error($"Could not find {UnityEngineDebugTypeName} type in runtime.");
                return false;
            }

            var evalOptions = session.Options.EvaluationOptions.AllowFullInvokes();

            try
            {
                if ((BreakpointTraceOutputSettings & BreakpointTraceOutput.DebugConsole) > 0)
                {
                    var lineEnding = message.EndsWith("\n") ? string.Empty : "\n";
                    myDebuggerSession.OnDebuggerOutput(false, message + lineEnding);
                }


                if ((BreakpointTraceOutputSettings & BreakpointTraceOutput.UnityOutput) > 0)
                {
                    var appDomainId = activeFrame.GetAppDomainId();
                    if (!myReifiedTypesLocalCache.TryGetValue(appDomainId, out var unityEngineDebugReifiedType))
                    {
                        unityEngineDebugReifiedType =
                            (IReifiedType<Value>)session.TypeUniverse.GetReifiedType(appDomainId, debugType);
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
            }
            catch (Exception e)
            {
                myLogger.Error(e, $"Failed to create {UnityEngineDebugTypeName}");
            }

            return true;
        }
    }
}