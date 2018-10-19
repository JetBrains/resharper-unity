using JetBrains.ReSharper.FeaturesTestFramework.ContextHighlighters;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.ShaderLab.Daemon.ContextHighlighters
{
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabPropertyUsageContextHighlighterTest : ContextHighlighterTestBase
    {
        protected override string RelativeTestDataPath => @"ShaderLab\" + base.RelativeTestDataPath;
        protected override string ExtraPath => @"PropertyUsage";

        [Test] public void TestPropertyUsage01() { DoNamedTest2(); }
        [Test] public void TestPropertyUsage02() { DoNamedTest2(); }
    }
}