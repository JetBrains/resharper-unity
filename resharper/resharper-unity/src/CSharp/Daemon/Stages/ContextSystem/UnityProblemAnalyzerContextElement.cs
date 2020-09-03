using System;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    [Flags]
    public enum UnityProblemAnalyzerContextElement : byte
    {
        NONE = 0,
        
        PERFORMANCE_CONTEXT = 1 << 0,
        BURST_CONTEXT = 1 << 1
    }
}