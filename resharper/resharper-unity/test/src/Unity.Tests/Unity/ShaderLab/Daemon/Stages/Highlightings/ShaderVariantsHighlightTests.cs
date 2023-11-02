using System.Collections.Generic;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.Caches;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Tests.UnityTestComponents;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Daemon.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Model;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Daemon.Stages.Highlightings
{
    [RequireHlslSupport, TestUnity, HighlightOnly(typeof(ImplicitlyEnabledShaderKeywordHighlight), typeof(EnabledShaderKeywordHighlight), typeof(DisabledShaderKeywordHighlight), typeof(SuppressedShaderKeywordHighlight))]
    [TestSetting(typeof(UnitySettings), nameof(UnitySettings.FeaturePreviewShaderVariantsSupport), true)]
    public class ShaderVariantsHighlightTests : HighlightingTestBase
    {
        private ShaderApi myShaderApi = ShaderApi.D3D11;
        private readonly List<string> myEnabledKeywords = new();
        
        protected override PsiLanguageType? CompilerIdsLanguage => CppLanguage.Instance;
        protected override string RelativeTestDataPath => @"ShaderLab\Daemon\Stages\Highlightings";

        public override void SetUp()
        {
            base.SetUp();
            myEnabledKeywords.Clear();
            myShaderApi = ShaderApi.D3D11;
        }

        [Test]
        public void TestImplicitHighlight() => DoTestSolution("ShaderVariants.hlsl", "FooBar.shader");
        
        [Test]
        public void TestSingleKeywordInShader() => DoTestSolution("SingleKeywordInShader.shader");
        
        [Test]
        public void TestPragmaHighlight() => DoTestSolution("FooBar.shader");
        
        [Test]
        public void TestEnabledKeywordsInShader()
        {
            myEnabledKeywords.Add("FOO");
            myEnabledKeywords.Add("B");
            myEnabledKeywords.Add("C");
            DoTestSolution("EnabledKeywordsInShader.shader");
        }
        
        [Test]
        public void TestShaderApiInShader()
        {
            DoTestSolution("ShaderApiInShader.shader");
        }
        
        [Test]
        public void TestShaderApiInShaderMetal()
        {
            myShaderApi = ShaderApi.Metal;
            DoTestSolution("ShaderApiInShaderMetal.shader");
        }

        protected override void DoTest(Lifetime lifetime, IProject project)
        {
            var enabledKeywords = project.GetComponent<TestEnabledShaderKeywordsProvider>().EnabledKeywords;
            project.GetComponent<TestShaderProgramInfoProvider>().ShaderApi = myShaderApi;
            enabledKeywords.UnionWith(myEnabledKeywords);
            try
            {
                project.GetComponent<CppGlobalCacheImpl>().ResetCache();
                base.DoTest(lifetime, project);
            }
            finally
            {
                enabledKeywords.Clear();
            }
        }
    }
}