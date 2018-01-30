using JetBrains.ReSharper.Daemon.VisualElements;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Tests.ShaderLab.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.ShaderLab.Psi.Colors
{
    [Category("ColorHighlighting")]
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabColorHighlightingTests : ShaderLabHighlightingTestBase
    {
        protected override bool ColorIdentifiers => true;
        protected override string RelativeTestDataPath => @"ShaderLab\Daemon\Stages\Colors";

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile)
        {
            return highlighting is ColorHighlighting;
        }

        [SetCulture("en-US")] [Test] public void TestPropertyColorCultureEn() { DoOneTest("PropertyColor"); }
        [SetCulture("de-DE")] [Test] public void TestPropertyColorCultureDe() { DoOneTest("PropertyColor"); }

        [SetCulture("en-US")] [Test] public void TestColorValuesCultureEn() { DoOneTest("ColorValues"); }
        [SetCulture("de-DE")] [Test] public void TestColorValuesCultureDe() { DoOneTest("ColorValues"); }

        [SetCulture("en-US")] [Test] public void TestEdgeCasesAndErrorsCultureEn() { DoOneTest("EdgeCasesAndErrors"); }
        [SetCulture("de-DE")] [Test] public void TestEdgeCasesAndErrorsCultureDe() { DoOneTest("EdgeCasesAndErrors"); }
    }
}