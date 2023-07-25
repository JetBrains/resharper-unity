using JetBrains.ReSharper.FeaturesTestFramework.CodeCleanup;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Feature.Services.CodeCleanup
{
    [TestUnity, RequireHlslSupport]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabCodeCleanupTest : CodeCleanupTestBase
    {
        protected override string RelativeTestDataPath => "ShaderLab/CodeCleanup";

        [TestCase("Test01")]
        public void Test(string testName) => DoOneTest(testName);
    }
}