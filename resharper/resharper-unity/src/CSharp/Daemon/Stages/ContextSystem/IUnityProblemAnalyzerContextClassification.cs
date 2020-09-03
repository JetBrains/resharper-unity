using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Collections;
using JetBrains.Diagnostics;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    /// <summary>
    /// Helps classify every context system element by it's context
    /// </summary>
    public interface IUnityProblemAnalyzerContextClassification
    {
        UnityProblemAnalyzerContextElement Context { get; }
    }

    public static class UnityProblemAnalyzerContextClassificationUtil
    {
        [Conditional("JET_MODE_ASSERT")]
        public static void AssertClassifications(this IEnumerable<IUnityProblemAnalyzerContextClassification> enumerable)
        {
            var elementToList = enumerable.GroupBy(t => t.Context).ToDictionary(t => t.Key, t => t.ToList());

            foreach (var (key, value) in elementToList)
                Assertion.Assert(value.Count == 1, $"{key} must have only 1 settings provider");
            
            // Assertion.Assert(elementToList.Count == UnityProblemAnalyzerContextElementUtil.UnityProblemAnalyzerContextSize, "number of classifications must be equal to number of contexts");
            
        }
    }
}