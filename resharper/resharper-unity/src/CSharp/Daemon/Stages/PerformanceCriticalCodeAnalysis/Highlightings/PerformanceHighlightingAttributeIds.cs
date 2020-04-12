namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings
{
    // Note that all attribute IDs should start with "ReSharper Unity " to appear properly in both Rider and ReSharper
    public static class PerformanceHighlightingAttributeIds
    {
        public const string CAMERA_MAIN = "ReSharper Unity Expensive Camera Main Usage";
        public const string COSTLY_METHOD_INVOCATION = "ReSharper Unity Expensive Method Invocation";
        public const string NULL_COMPARISON = "ReSharper Unity Expensive Null Comparison";
        public const string INEFFICIENT_MULTIPLICATION_ORDER = "ReSharper Unity Inefficient Multiplication Order";
        public const string INEFFICIENT_MULTIDIMENSIONAL_ARRAYS_USAGE = "ReSharper Unity Inefficient Multidimensional Array Usage";
        public const string PERFORMANCE_CRITICAL_METHOD_HIGHLIGHTER = "ReSharper Unity Performance Critical Line Marker";
    }
}