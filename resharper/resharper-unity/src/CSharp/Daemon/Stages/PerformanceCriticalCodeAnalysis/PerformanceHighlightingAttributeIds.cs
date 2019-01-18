namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis
{
    // Note that all attribute IDs should start with "ReSharper Unity " to appear properly in both Rider and ReSharper
    public static class PerformanceHighlightingAttributeIds
    {
        public const string CAMERA_MAIN = "ReSharper Unity Expensive Camera Main Usage";
        public const string COSTLY_METHOD_INVOCATION = "ReSharper Unity Expensive Method Invocation";
        public const string NULL_COMPARISON = "ReSharper Unity Expensive Null Comparison";
        public const string COSTLY_METHOD_HIGHLIGHTER = "ReSharper Unity Performance Critical Context";
    }
}