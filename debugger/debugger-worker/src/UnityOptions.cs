using System;
using Mono.Debugging.Autofac;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger
{
    public interface IUnityOptions
    {
        bool ExtensionsEnabled { get; }
    }

    [DebuggerGlobalComponent]
    public class UnityOptions : IUnityOptions
    {
        public UnityOptions()
        {
            ExtensionsEnabled = Environment.GetEnvironmentVariable("_RIDER_UNITY_ENABLE_DEBUGGER_EXTENSIONS") == "1";
        }

        public bool ExtensionsEnabled { get; }
    }
}