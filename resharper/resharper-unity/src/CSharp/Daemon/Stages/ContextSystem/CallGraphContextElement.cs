using System;
using System.Diagnostics.CodeAnalysis;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum CallGraphContextElement : byte
    {
        NONE = 0,
        
        PERFORMANCE_CRITICAL_CONTEXT = 1 << 0,
        BURST_CONTEXT = 1 << 1,
        EXPENSIVE_CONTEXT = 1 << 2
    }
    
    public static class CallGraphContextElementUtil
    {
        public const int CONTEXTS_COUNT = 3;

        public const CallGraphContextElement ALL = CallGraphContextElement.PERFORMANCE_CRITICAL_CONTEXT |
                                                   CallGraphContextElement.BURST_CONTEXT | 
                                                   CallGraphContextElement.EXPENSIVE_CONTEXT;
    }
}