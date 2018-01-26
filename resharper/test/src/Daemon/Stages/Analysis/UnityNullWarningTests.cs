using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Daemon.Stages.Analysis
{
    [TestUnity]
    public class UnityNullWarningTests : CSharpHighlightingTestBase
    {
        protected override string RelativeTestDataPath => @"daemon\Stages\Analysis";

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile)
        {
            return highlighting is UnityNullCoalescingWarning || highlighting is UnityNullPropagationWarning;
        }

        [Test] public void TestUnityNullCoalescingWarning() { DoNamedTest2(); }
        [Test] public void TestUnityNullPropagationWarning() { DoNamedTest2(); }
    }
}
