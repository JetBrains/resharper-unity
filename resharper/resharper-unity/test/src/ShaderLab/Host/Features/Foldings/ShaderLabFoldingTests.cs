using JetBrains.ReSharper.Daemon.CodeFolding;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Tests.ShaderLab.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.ShaderLab.Host.Features.Foldings
{
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabFoldingTests : ShaderLabHighlightingTestBase<CodeFoldingHighlighting>
    {
        protected override string RelativeTestDataPath => @"ShaderLab\Foldings";

        [Ignore("Host features are not available to tests")]
        [Test] public void TestFoldings() { DoNamedTest2(); }
    }
}