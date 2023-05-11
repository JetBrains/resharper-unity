using JetBrains.ProjectModel;
using JetBrains.ReSharper.FeaturesTestFramework.Breadcrumbs;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Feature.Services.Breadcrumbs
{
    [RequireHlslSupport, TestFileExtension(CppProjectFileType.HLSL_EXTENSION)]
    public class HlslBreadcrumbsTest : BreadcrumbsTestBase
    {
        protected override string RelativeTestDataPath => @"ShaderLab\Breadcrumbs";

        [Test]
        public void TestHLSLFile() => DoNamedTest();
    }
}