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
        private readonly IDebuggerSessionInternal mySession;

        public UnityDebugLogger(IDebuggerSessionInternal session)
        {
            mySession = session;
        }
        
        public void Handle(BreakEvent be, IStackFrame activeFrame, string message)
        {
            mySession.Evaluators.Evaluate(activeFrame,
                    new EvaluationExpression($"UnityEngine.Debug.Log(@\"{message}\")", null, null), allowInvokes: true)
                .GetPrimaryRole(mySession.EvaluationOptions.AllowFullInvokes());
            
        }
    }
}