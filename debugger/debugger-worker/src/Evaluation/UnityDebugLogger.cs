using JetBrains.Debugger.Model.Plugins.Unity;
using Mono.Debugging.Autofac;
using Mono.Debugging.Client;
using Mono.Debugging.Client.CallStacks;
using Mono.Debugging.Client.Values.Render;
using Mono.Debugging.Evaluation;
using Mono.Debugging.Soft;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Evaluation
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class UnityDebugLogger : IBreakpointTraceHandler
    {
        public UnityDebugLogger(IDebuggerSession session, IDebugSessionFrontend debugSessionFrontend)
        {
            mySession = session as IDebuggerSessionInternal;
            myIsUnityDebugSession = debugSessionFrontend is RiderDebuggerSessionFrontend riderDebuggerSessionFrontend
                                    && riderDebuggerSessionFrontend.SessionModel.StartInfo is UnityStartInfo;
        }

        private readonly IDebuggerSessionInternal? mySession;
        private readonly bool myIsUnityDebugSession;

        public bool Handle(BreakEvent be, IStackFrame activeFrame, string message)
        {
            if (myIsUnityDebugSession)
            {
                mySession?.Evaluators.Evaluate(activeFrame,
                        new EvaluationExpression($"UnityEngine.Debug.Log(@\"{message}\")", null, null),
                        allowInvokes: true)
                    .GetPrimaryRole(mySession.EvaluationOptions.AllowFullInvokes());
                return true;
            }

            return false;
        }
    }
}