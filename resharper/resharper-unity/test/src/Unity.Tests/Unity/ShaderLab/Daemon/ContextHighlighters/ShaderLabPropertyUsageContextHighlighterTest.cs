using JetBrains.ReSharper.FeaturesTestFramework.ContextHighlighters;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Daemon.ContextHighlighters
{
    [RequireHlslSupport]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabPropertyUsageContextHighlighterTest : ContextHighlighterTestBase
    {
        protected override string RelativeTestDataPath => @"ShaderLab\" + base.RelativeTestDataPath;
        protected override string ExtraPath => @"PropertyUsage";

        [Test] public void TestPropertyUsage01() { DoNamedTest2(); }
        [Test] public void TestPropertyUsage02() { DoNamedTest2(); }
    }
}