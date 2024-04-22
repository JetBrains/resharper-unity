using System;
using System.Linq;
using System.Threading;
using JetBrains.Debugger.Model.Plugins.Unity;
using JetBrains.Debugger.Worker.Plugins.Unity.Resources;
using JetBrains.Lifetimes;
using JetBrains.Util;
using Mono.Debugger.Soft;
using Mono.Debugging.Autofac;
using Mono.Debugging.Client;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Soft;
using Mono.Debugging.TypeSystem.KnownTypes;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Breakpoints
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class UnityPausePointHandler : IBreakpointAdditionalActionHandler
    {
        private readonly ILogger myLogger;
        private readonly SoftDebuggerSession mySession;
        private readonly bool myIsEnabled;
        private UnityPausePointHelper? myHelper;
        private readonly int myEvaluationTimeout;
        private readonly IKnownTypes<Value> myKnownTypes;
        private readonly string myAssemblyAbsolutePath = string.Empty;

        public UnityPausePointHandler(SoftDebuggerSession session,
            IUnityOptions unityOptions,
            ISessionCreationInfo creationInfo,
            Lifetime lifetime,
            IKnownTypes<Value> knownTypes,
            ILogger logger
        )
        {
            myLogger = logger;
            mySession = session;

            if (creationInfo.StartInfo is UnityStartInfo unityStartInfo)
            {
                myIsEnabled = unityOptions.ExtensionsEnabled;
                var unityBundleInfo =
                    unityStartInfo.Bundles.FirstOrDefault(b => b.Id.Equals(UnityPausePointHelper.AssemblyName));
                if (unityBundleInfo != null)
                    myAssemblyAbsolutePath = unityBundleInfo.AbsolutePath;
                else
                {
                    myIsEnabled = false;
                    myAssemblyAbsolutePath = string.Empty;
                    myLogger.Error($"UnityBundles don't contain required one '{UnityPausePointHelper.AssemblyName}'");
                }
                
                myEvaluationTimeout = unityOptions.ForcedTimeoutForAdvanceUnityEvaluation;
            }
            
            myKnownTypes = knownTypes;
        }

        public bool Handle<TModule>(BreakEvent breakEvent, BreakEventInfo<TModule> breakEventInfo,
            IStackFrame activeFrame)
        {
            if (!myIsEnabled) return false;

            if (breakEvent is not LineBreakpoint lineBreakpoint)
                return false;

            if (lineBreakpoint.TryGetAdditionalData<UnityPausePointAdditionalData>() == null)
                return false;

            var evaluationParameters = mySession.EvaluationOptions
                .AllowFullInvokes()
                .WithOverridden(o => { o.EvaluationTimeout = myEvaluationTimeout; });

            try
            {
                if (myHelper == null || activeFrame.GetAppDomainId() != myHelper.DomainTypes.AppDomainId)
                    myHelper = UnityPausePointHelper.CreateHelper(activeFrame, evaluationParameters,
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
                    Strings.UnityPausePointExceptionOccured.FormatEx(e.Message));
            }

            return true;
        }
    }
}