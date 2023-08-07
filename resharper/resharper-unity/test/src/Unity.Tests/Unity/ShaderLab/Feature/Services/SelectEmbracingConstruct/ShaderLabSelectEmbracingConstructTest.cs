using JetBrains.ReSharper.Feature.Services.Tests.FeatureServices.SelectEmbracingConstruct;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Feature.Services.SelectEmbracingConstruct
{
    [TestUnity, TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION), RequireHlslSupport]
    public class ShaderLabSelectEmbracingConstructTest : SelectEmbracingConstructTestBase
    {
        protected override string RelativeTestDataPath => @"ShaderLab\SelectEmbracingConstruct";

        [TestCase("Test01")]
        [TestCase("Test02")]
        [TestCase("Test03")]
        [TestCase("Test04")]
        [TestCase("Test05")]
        public void Test(string testName) => DoOneTest(testName);
    }
}