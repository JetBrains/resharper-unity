using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Daemon.Stages.Analysis
{
    [TestUnity]
    public class UnityNullCoalescingWarningTests : CSharpHighlightingTestBase
    {
        protected override string RelativeTestDataPath => @"daemon\Stages\Analysis";

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile)
        {
            return highlighting is UnityNullCoalescingWarning;
        }

        [Test] public void TestUnityNullCoalescingWarning() { DoNamedTest2(); }
    }
}
