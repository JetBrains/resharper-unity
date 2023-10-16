using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.Caches;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Daemon.Highlightings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Language;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Daemon.Stages.Highlightings
{
    [RequireHlslSupport, TestUnity, HighlightOnly(typeof(ShaderVariantHighlight))]
    public class ShaderVariantsHighlightTests : HighlightingTestBase
    {
        protected override PsiLanguageType? CompilerIdsLanguage => CppLanguage.Instance;
        protected override string RelativeTestDataPath => @"ShaderLab\Daemon\Stages\Highlightings";

        [TestCase("ShaderVariants.hlsl")]
        public void TestShaderVariantsHighlight(string testFile) => DoTestSolution(testFile, "FooBar.shader");

        protected override void DoTest(Lifetime lifetime, IProject project)
        {
            project.GetComponent<CppGlobalCacheImpl>().ResetCache();
            base.DoTest(lifetime, project);
        }
    }
}