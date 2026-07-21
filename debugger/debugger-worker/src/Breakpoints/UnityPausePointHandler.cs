using System;
using System.Linq;
using JetBrains.Debugger.Model.Plugins.Unity;
using JetBrains.Debugger.Worker.Plugins.Unity.Resources;
using JetBrains.Debugger.Worker.Plugins.Unity.SessionStartup;
using JetBrains.Lifetimes;
using JetBrains.Util;
using Mono.Debugger.Soft;
using Mono.Debugging.Autofac;
using Mono.Debugging.Client;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Soft;
using Mono.Debugging.TypeSystem.KnownTypes;
using Mono.Debugging.Win32;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Breakpoints
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class MonoUnityPausePointHandler : UnityPausePointHandler<Value>
    {
        public MonoUnityPausePointHandler(SoftDebuggerSession session, IUnityOptions unityOptions, ISessionCreationInfo creationInfo, Lifetime lifetime, IKnownTypes<Value> knownTypes, ILogger logger) : base(session, unityOptions, creationInfo, lifetime, knownTypes, logger)
        {
        }
    }

    // TODO: actually test this when we get CoreCLR editor
    [DebuggerSessionComponent(typeof(CorDebuggerType))]
    public class CorUnityPausePointHandler : UnityPausePointHandler<ICorValue>
    {
        public CorUnityPausePointHandler(CorDebuggerSession session, IUnityOptions unityOptions, ISessionCreationInfo creationInfo, Lifetime lifetime, IKnownTypes<ICorValue> knownTypes, ILogger logger) : base(session, unityOptions, creationInfo, lifetime, knownTypes, logger)
        {
        }
    }

    public class UnityPausePointHandler<TValue> : IBreakpointAdditionalActionHandler where TValue : class
    {
        private readonly ILogger myLogger;
        private readonly DebuggerSession<TValue> mySession;
        
        private UnityPausePointHelper<TValue>? myHelper;
        private readonly IKnownTypes<TValue> myKnownTypes;
        private readonly string myAssemblyAbsolutePath = string.Empty;
        private readonly IUnityOptions myUnityOptions;

        private int EvaluationTimeout => myUnityOptions.ForcedTimeoutForAdvanceUnityEvaluation;
        private bool IsEnabled => !string.IsNullOrEmpty(myAssemblyAbsolutePath) && myUnityOptions.ExtensionsEnabled;
        
        public UnityPausePointHandler(DebuggerSession<TValue> session,
            IUnityOptions unityOptions,
            ISessionCreationInfo creationInfo,
            Lifetime lifetime,
            IKnownTypes<TValue> knownTypes,
            ILogger logger
        )
        {
            myLogger = logger;
            mySession = session;
            myUnityOptions = unityOptions;

            if (creationInfo.StartInfo is UnityStartInfo unityStartInfo)
            {
                var unityBundleInfo =
                    unityStartInfo.GetProjectData().Bundles.FirstOrDefault(b => b.Id.Equals(UnityPausePointHelper<TValue>.AssemblyName));
                if (unityBundleInfo != null)
                {
                    myAssemblyAbsolutePath = unityBundleInfo.AbsolutePath;
                }
                else
                {
                    myAssemblyAbsolutePath = string.Empty;
                    myLogger.Error($"UnityBundles don't contain required one '{UnityPausePointHelper<TValue>.AssemblyName}'");
                }
            }
            
            myKnownTypes = knownTypes;
        }

        public bool Handle<TModule>(BreakEvent breakEvent, BreakEventInfo<TModule> breakEventInfo,
            IStackFrame activeFrame)
        {
            if (!IsEnabled) return false;

            if (breakEvent is not LineBreakpoint lineBreakpoint)
                return false;

            if (lineBreakpoint.TryGetAdditionalData<UnityPausePointAdditionalData>() == null)
                return false;

            var evaluationParameters = mySession.EvaluationOptions
                .AllowFullInvokes()
                .WithOverridden(o => { o.EvaluationTimeout = EvaluationTimeout; });

            try
            {
                if (myHelper == null || activeFrame.GetAppDomainId() != myHelper.DomainTypes.AppDomainId)
                    myHelper = UnityPausePointHelper<TValue>.CreateHelper(activeFrame, evaluationParameters,
                        myKnownTypes, myAssemblyAbsolutePath);
            }
            catch (Exception e)
            {
                myLogger.LogException(e);
                return true;
            }

            try
            {
                myHelper.RequestPause().Call(activeFrame, evaluationParameters);
                breakEventInfo.UpdateLastTraceValue(activeFrame, Strings.UnityPausePointHit);
            }
            catch (Exception e)
            {
                myLogger.LogException(e);
                breakEventInfo.UpdateLastTraceValue(activeFrame,
                    string.Format(Strings.UnityPausePointExceptionOccured, e.Message));
            }

            return true;
        }
    }
}