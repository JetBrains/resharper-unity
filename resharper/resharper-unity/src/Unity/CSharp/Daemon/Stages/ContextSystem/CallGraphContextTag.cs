using System;
using System.Diagnostics.CodeAnalysis;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    // enum only because changing context with strings is VERY expensive
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum CallGraphContextTag : byte
    {
        NONE = 0,
        
        PERFORMANCE_CRITICAL_CONTEXT = 1 << 0,
        BURST_CONTEXT = 1 << 1,
        EXPENSIVE_CONTEXT = 1 << 2
    }
    
    public static class CallGraphContextTagUtil
    {
        public const int CONTEXTS_COUNT = 3;

        public const CallGraphContextTag ALL = CallGraphContextTag.PERFORMANCE_CRITICAL_CONTEXT |
                                                   CallGraphContextTag.BURST_CONTEXT | 
                                                   CallGraphContextTag.EXPENSIVE_CONTEXT;
    }
}