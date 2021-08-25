using JetBrains.Debugger.Model.Plugins.Unity;
using Mono.Debugging.Autofac;
using Mono.Debugging.Soft;

namespace JetBrains.Debugger.Worker.Plugins.Unity.SessionStartup
{
    [DebuggerGlobalComponent]
    public class UnityStartInfoHandler : UnityStartInfoHandlerBase<UnityStartInfo>
    {
        public UnityStartInfoHandler() : base(SoftDebuggerType.Instance)
        {
        }
    }
}