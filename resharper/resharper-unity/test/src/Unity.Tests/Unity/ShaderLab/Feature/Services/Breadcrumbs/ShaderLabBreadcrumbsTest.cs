using JetBrains.ReSharper.FeaturesTestFramework.Breadcrumbs;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Feature.Services.Breadcrumbs
{
    [RequireHlslSupport, TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabBreadcrumbsTest : BreadcrumbsTestBase
    {
        protected override string RelativeTestDataPath => @"ShaderLab\Breadcrumbs";

        [TestCase("TestCGProgramBlock")]
        public void TestBreadcrumbs(string testName) => DoOneTest(testName);
    }
}