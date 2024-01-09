using System;
using System.Linq;
using JetBrains.Debugger.Model.Plugins.Unity;
using JetBrains.Rider.Model.DebuggerWorker;
using Mono.Debugging.Autofac;
using Mono.Debugging.Client;
using Mono.Debugging.Soft;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Breakpoints
{
    [DebuggerSessionComponent(typeof(SoftDebuggerType))]
    public class UnityPausePointAdditionalDataProvider : IAdditionalBreakPointDataProvider
    {
        public IBreakpointAdditionalData? GetData(BreakpointModel model)
        {
            if(model is LineBreakpointModel lineBreakpointModel && lineBreakpointModel.AdditionalActions?.OfType<UnityPausepointAdditionalAction>().FirstOrDefault() != null)
                return  new UnityPausePointAdditionalData();
            return null;
        }
    }


    public class UnityPausePointAdditionalData : IBreakpointAdditionalData
    {
    }
}