using JetBrains.Debugger.Model.Plugins.Unity;
using JetBrains.Debugger.Worker.SessionStartup;
using Mono.Debugging.Autofac;

namespace JetBrains.Debugger.Worker.Plugins.Unity.SessionStartup
{
    public abstract class UnityStartInfoHandlerBase<T> : ModelStartInfoHandlerBase<T>
        where T : UnityStartInfoBase
    {
        protected UnityStartInfoHandlerBase(DebuggerType debuggerType) : base(debuggerType)
        {
        }


        // We have to inject this or we get a circular reference - options depends on DebuggerWorker which depends on
        // start info handlers
        [Injected] internal IUnityOptions UnityOptions { get; set; } = null!;
    }
}