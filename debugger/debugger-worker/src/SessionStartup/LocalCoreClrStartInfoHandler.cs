using JetBrains.Debugger.Model.Plugins.Unity;
using JetBrains.Debugger.Worker.SessionStartup;
using Mono.Debugging.Autofac;
using Mono.Debugging.Client;
using Mono.Debugging.Client.DebuggerOptions;
using Mono.Debugging.Win32;

namespace JetBrains.Debugger.Worker.Plugins.Unity.SessionStartup
{
    [DebuggerGlobalComponent]
    public class LocalCoreClrStartInfoHandler : UnityStartInfoHandlerBase<UnityLocalCoreClrStartInfo>
    {
        public LocalCoreClrStartInfoHandler() : base(GenericCoreClrDebuggerType.Instance)
        {
        }
        
        // TODO: Should EnC be supported?
        // CorDebugAttachHandlerBase handles this
        // See also DotNetCoreAttachSuspendedHandler

        protected override IDebuggerSessionStarter GetSessionStarter(UnityLocalCoreClrStartInfo startInfo,
            IDebuggerSessionOptions debuggerSessionOptions)
        {
            return new AttachSessionStarter(new ProcessInfo(startInfo.ProcessId, ""), debuggerSessionOptions);
        }
    }
}
