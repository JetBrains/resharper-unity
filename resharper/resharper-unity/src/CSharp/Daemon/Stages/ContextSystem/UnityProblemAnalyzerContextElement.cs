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
    
    public static class UnityProblemAnalyzerContextElementUtil
    {
        public const int CONTEXTS_COUNT = 2;

        public const UnityProblemAnalyzerContextElement ALL = UnityProblemAnalyzerContextElement.PERFORMANCE_CONTEXT |
                                                              UnityProblemAnalyzerContextElement.BURST_CONTEXT;
    }
}