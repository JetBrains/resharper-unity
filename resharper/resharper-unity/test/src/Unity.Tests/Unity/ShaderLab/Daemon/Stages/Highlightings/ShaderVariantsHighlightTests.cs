using System.Collections.Generic;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.Caches;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Daemon.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;
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
        private ShaderPlatform myShaderPlatform = ShaderPlatform.Desktop;
        private readonly List<string> myEnabledKeywords = new();
        
        protected override PsiLanguageType? CompilerIdsLanguage => CppLanguage.Instance;
        protected override string RelativeTestDataPath => @"ShaderLab\Daemon\Stages\Highlightings";

        public override void TearDown()
        {
            myEnabledKeywords.Clear();
            var shaderVariantsManager = Solution.GetComponent<ShaderVariantsManager>();
            myShaderApi = ShaderApiDefineSymbolDescriptor.DefaultValue;
            myShaderPlatform = ShaderPlatformDefineSymbolDescriptor.DefaultValue;
            shaderVariantsManager.SetShaderApi(myShaderApi);
            shaderVariantsManager.SetShaderPlatform(myShaderPlatform);
            base.TearDown();
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
        public void TestShaderApiInShader() => DoTestSolution("ShaderApiInShader.shader");

        [Test]
        public void TestShaderApiInShaderMetal()
        {
            myShaderApi = ShaderApi.Metal;
            DoTestSolution("ShaderApiInShaderMetal.shader");
        }
        
        [Test]
        public void TestShaderPlatformInShader() => DoTestSolution("ShaderPlatformInShader.shader");
        
        [Test]
        public void TestShaderPlatformInShaderMobile()
        {
            myShaderPlatform = ShaderPlatform.Mobile;
            DoTestSolution("ShaderPlatformInShaderMobile.shader");
        }

        protected override void DoTest(Lifetime lifetime, IProject project)
        {
            var shaderVariantsManager = project.GetComponent<ShaderVariantsManager>();
            foreach (var keyword in myEnabledKeywords) 
                shaderVariantsManager.SetKeywordEnabled(keyword, true);
            shaderVariantsManager.SetShaderApi(myShaderApi);
            shaderVariantsManager.SetShaderPlatform(myShaderPlatform);

            try
            {
                project.GetComponent<CppGlobalCacheImpl>().ResetCache();
                base.DoTest(lifetime, project);
            }
            finally
            {
                shaderVariantsManager.ResetAllKeywords();
            }
        }
    }
}