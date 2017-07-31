#if RIDER

using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Tests.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Host.Features.SyntaxHighlighting.ShaderLab
{
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabSyntaxHighlightingTests : ShaderLabHighlightingTestBase
    {
        protected override string RelativeTestDataPath => @"syntaxHighlighting\shaderlab";

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile)
        {
            // TODO: highlighting is ReSharperSyntaxHighlighting
            return highlighting.GetType().Name == "ReSharperSyntaxHighlighting";
        }

        [Test] public void TestSyntaxHighlighting() { DoNamedTest2(); }
    }
}

#endif