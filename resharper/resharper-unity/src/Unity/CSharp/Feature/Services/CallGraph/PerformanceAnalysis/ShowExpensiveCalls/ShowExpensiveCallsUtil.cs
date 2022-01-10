using System;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.ShowExpensiveCalls
{
    public class ShowExpensiveCallsUtil
    {
        public const string OUTGOING_CALLS_MESSAGE = "Show outgoing Expensive calls";
        public const string INCOMING_CALLS_MESSAGE = "Show incoming Expensive calls";
        public const string CONTEXT_ACTION_DESCRIPTION = "Show Expensive calls actions";

        public static string GetExpensiveShowCallsText(ShowCallsType type)
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