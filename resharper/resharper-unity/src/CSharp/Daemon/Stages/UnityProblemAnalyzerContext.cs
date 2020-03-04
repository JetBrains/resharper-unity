using System;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    [Flags]
    public enum UnityProblemAnalyzerContext : byte
    {
        NONE = 0,
        
        PERFOMANCE_CONTEXT = 1 << 0
    }
}