using JetBrains.ReSharper.Daemon.VisualElements;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Daemon.Highlighting
{
    [Category("ColorHighlighting")]
    [TestUnity]
    public class UnityColorHiglightingTest : CSharpHighlightingTestBase
    {
        protected override bool ColorIdentifiers => true;
        protected override string RelativeTestDataPath => @"daemon\Highlighting\Colors";

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile)
        {
            return highlighting is ColorHighlighting;
        }

        [Test] public void Test01() { DoNamedTest(); }
    }
}