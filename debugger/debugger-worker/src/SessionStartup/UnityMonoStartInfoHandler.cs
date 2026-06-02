using JetBrains.Debugger.Model.Plugins.Unity;
using Mono.Debugging.Autofac;

namespace JetBrains.Debugger.Worker.Plugins.Unity.SessionStartup
{
    [DebuggerGlobalComponent]
    public class UnityMonoStartInfoHandler : UnityMonoStartInfoHandlerBase<UnityMonoStartInfo>
    {
    }
}
