using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.Caches;
using JetBrains.ReSharper.FeaturesTestFramework.Refactorings;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Refactorings.Rename
{
    [Category("Cpp.HLSL"), Category("Cpp.Unity")]
    [TestUnity, RequireHlslSupport, TestFileExtension(CppProjectFileType.HLSL_EXTENSION)]
    public class CppHlslRenameTest : RenameTestBase
    {
        protected override string RelativeTestDataPath => @"ShaderLab\Refactorings\Rename\Hlsl";

        [TestCase("Test01")]
        [TestCase("Test02")]
        public void TestRenamingInCompute(string testName) => DoTestSolution(testName + CppProjectFileType.COMPUTE_EXTENSION);

        [TestCase("Test03")]
        [TestCase("Test04")]
        [TestCase("Test05")]
        [TestCase("Test06")]
        [TestCase("Test07")]
        [TestCase("Test08")]
        [TestCase("Test09")]
        [TestCase("Test10")]
        [TestCase("Test11")]
        [TestCase("Test12")]
        [TestCase("Test13")]
        [TestCase("Test14")]
        public void TestRenaming(string testName) => DoOneTest(testName);

        protected override void DoTest(Lifetime lifetime, IProject testProject)
        {
            testProject.GetComponent<CppGlobalCacheImpl>().ResetCache();
            base.DoTest(lifetime, testProject);
        }
    }
}