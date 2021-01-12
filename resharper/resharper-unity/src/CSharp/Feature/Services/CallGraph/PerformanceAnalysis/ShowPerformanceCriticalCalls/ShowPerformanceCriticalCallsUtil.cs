using System;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.ShowPerformanceCriticalCalls
{
    public class ShowPerformanceCriticalCallsUtil
    { 
        public const string OUTGOING_CALLS_MESSAGE = "Show outgoing Performance critical calls";
        public const string INCOMING_CALLS_MESSAGE = "Show incoming Performance critical calls";
        public const string CONTEXT_ACTION_DESCRIPTION = "Show Performance critical calls actions";

        public static string GetPerformanceCriticalShowCallsText(ShowCallsType type)
        {
            switch (type)
            {
                case ShowCallsType.INCOMING:
                    return INCOMING_CALLS_MESSAGE;
                case ShowCallsType.OUTGOING:
                    return OUTGOING_CALLS_MESSAGE;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}