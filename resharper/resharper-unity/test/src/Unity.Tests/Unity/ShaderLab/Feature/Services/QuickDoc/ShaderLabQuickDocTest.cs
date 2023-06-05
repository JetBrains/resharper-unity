using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Tests.TestFramework;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Feature.Services.QuickDoc
{
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabQuickDocTest : QuickDocTestBase
    {
        protected override string RelativeTestDataPath => @"ShaderLab\QuickDoc";

        protected override void TestAdditionalInfo(IDeclaredElement declaredElement, IProjectFile projectFile)
        {
        }

        [TestCase("ShaderLabKeyword")] 
        public void TestQuickDoc(string testName) { DoOneTest(testName); }
    }
}