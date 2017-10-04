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

        [Test] public void TestPropertyColor() { DoNamedTest2(); }
        [Test] public void TestColorValues() { DoNamedTest2(); }
        [Test] public void TestEdgeCasesAndErrors() { DoNamedTest2(); }
    }
}