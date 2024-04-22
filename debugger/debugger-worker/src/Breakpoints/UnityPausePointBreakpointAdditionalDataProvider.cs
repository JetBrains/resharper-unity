using JetBrains.Debugger.Model.Plugins.Unity;
using JetBrains.Rider.Model.DebuggerWorker;
using Mono.Debugging.Autofac;
using Mono.Debugging.Client;
using Mono.Debugging.Soft;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Breakpoints
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class UnityPausePointBreakpointAdditionalDataProvider : IBreakpointAdditionalDataProvider
    {
        public IBreakpointAdditionalData? GetData(BreakpointAdditionalDataModel additionalDataModel)
        {
            return additionalDataModel is UnityPausepointAdditionalDataModel
                ? new UnityPausePointAdditionalData()
                : null;
        }
    }

    public class UnityPausePointAdditionalData : IBreakpointCustomActionAdditionalData
    {
    }
}