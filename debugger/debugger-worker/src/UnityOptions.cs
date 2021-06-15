using JetBrains.Collections.Viewable;
using Mono.Debugging.Autofac;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger
{
    public interface IUnityOptions
    {
        bool ExtensionsEnabled { get; }
        bool IgnoreBreakOnUnhandledExceptionsForIl2Cpp { get; }
    }

    [DebuggerGlobalComponent]
    public class UnityOptions : IUnityOptions
    {
        private readonly UnityDebuggerWorkerHost myHost;

        public UnityOptions(UnityDebuggerWorkerHost host)
        {
            myHost = host;
        }

        public bool ExtensionsEnabled => myHost.Model.ShowCustomRenderers.HasTrueValue();

        public bool IgnoreBreakOnUnhandledExceptionsForIl2Cpp =>
            myHost.Model.IgnoreBreakOnUnhandledExceptionsForIl2Cpp.Value;
    }
}