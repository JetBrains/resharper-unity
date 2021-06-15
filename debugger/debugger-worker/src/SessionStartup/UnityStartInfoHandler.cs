using JetBrains.Rider.Model.Unity.DebuggerWorker;
using Mono.Debugging.Autofac;
using Mono.Debugging.Soft;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.SessionStartup
{
    [DebuggerGlobalComponent]
    public class UnityStartInfoHandler : UnityStartInfoHandlerBase<UnityStartInfo>
    {
        public UnityStartInfoHandler() : base(SoftDebuggerType.Instance)
        {
        }
    }
}