using JetBrains.ReSharper.Daemon.SyntaxHighlighting;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Tests.ShaderLab.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.ShaderLab.Host.Features.SyntaxHighlighting
{
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabSyntaxHighlightingTests : ShaderLabHighlightingTestBase<ReSharperSyntaxHighlighting>
    {
        protected override string RelativeTestDataPath => @"ShaderLab\SyntaxHighlighting";

        [Ignore("Host features are not available to tests")]
        [Test] public void TestSyntaxHighlighting() { DoNamedTest2(); }
    }
}