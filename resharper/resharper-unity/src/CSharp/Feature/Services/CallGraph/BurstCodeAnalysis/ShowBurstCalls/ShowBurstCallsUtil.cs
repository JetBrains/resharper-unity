using System;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.BurstCodeAnalysis.ShowBurstCalls
{
    internal static class ShowBurstCallsUtil
    {
        public const string OUTGOING_CALLS_MESSAGE = "Show outgoing Burst calls";
        public const string INCOMING_CALLS_MESSAGE = "Show incoming Burst calls";
        public const string CONTEXT_ACTION_DESCRIPTION = "Show Burst calls actions";

        public static string GetBurstShowCallsText(ShowCallsType type)
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