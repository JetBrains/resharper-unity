#if RIDER

using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Tests.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Host.Features.Foldings.ShaderLab
{
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabFoldingTests : ShaderLabHighlightingTestBase
    {
        protected override string RelativeTestDataPath => @"foldings\shaderlab";

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile)
        {
            // TODO: highlighting is CodeFoldingHighlighting
            return highlighting.GetType().Name == "CodeFoldingHighlighting";
        }

        [Test] public void TestFoldings() { DoNamedTest2(); }
    }
}

#endif