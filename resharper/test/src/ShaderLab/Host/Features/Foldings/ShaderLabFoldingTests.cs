#if RIDER

using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Tests.ShaderLab.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.ShaderLab.Host.Features.Foldings
{
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADER_EXTENSION)]
    public class ShaderLabFoldingTests : ShaderLabHighlightingTestBase
    {
        protected override string RelativeTestDataPath => @"ShaderLab\Foldings";

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile)
        {
            // TODO: highlighting is CodeFoldingHighlighting
            return highlighting.GetType().Name == "CodeFoldingHighlighting";
        }

        [Test] public void TestFoldings() { DoNamedTest2(); }
    }
}

#endif