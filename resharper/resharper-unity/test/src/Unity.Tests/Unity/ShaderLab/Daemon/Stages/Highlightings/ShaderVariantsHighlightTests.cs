using JetBrains.ReSharper.Feature.Services.Cpp.Caches;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
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
    public class ShaderVariantsHighlightTests : HighlightingTestBase
    {
        protected override PsiLanguageType? CompilerIdsLanguage => CppLanguage.Instance;
        protected override string RelativeTestDataPath => @"ShaderLab\Daemon\Stages\Highlightings";

        [Test, ShaderVariantsTest]
        public void TestImplicitHighlight() => DoTestSolution("ShaderVariants.hlsl", "FooBar.shader");
        
        [Test, ShaderVariantsTest]
        public void TestSingleKeywordInShader() => DoTestSolution("SingleKeywordInShader.shader");
        
        [Test, ShaderVariantsTest]
        public void TestPragmaHighlight() => DoTestSolution("FooBar.shader");
        
        [Test, ShaderVariantsTest(enabledKeywords: ["FOO", "B", "C"])]
        public void TestEnabledKeywordsInShader() => DoTestSolution("EnabledKeywordsInShader.shader");

        [Test, ShaderVariantsTest]
        public void TestShaderApiInShader() => DoTestSolution("ShaderApiInShader.shader");

        [Test, ShaderVariantsTest(shaderApi: ShaderApi.Metal)]
        public void TestShaderApiInShaderMetal() => DoTestSolution("ShaderApiInShaderMetal.shader");

        [Test, ShaderVariantsTest]
        public void TestShaderPlatformInShader() => DoTestSolution("ShaderPlatformInShader.shader");
        
        [Test, ShaderVariantsTest(shaderPlatform: ShaderPlatform.Mobile)]
        public void TestShaderPlatformInShaderMobile() => DoTestSolution("ShaderPlatformInShaderMobile.shader");

        private class ShaderVariantsTestAttribute(ShaderApi shaderApi = ShaderApi.D3D11, ShaderPlatform shaderPlatform = ShaderPlatform.Desktop, params string[] enabledKeywords): TestAspectAttribute
        {
            protected override void OnBeforeTestExecute(TestAspectContext context)
            {
                var shaderVariantsManager = context.TestProject.GetComponent<ShaderVariantsManager>();
                foreach (var keyword in enabledKeywords) 
                    shaderVariantsManager.SetKeywordEnabled(keyword, true);
                shaderVariantsManager.SetShaderApi(shaderApi);
                shaderVariantsManager.SetShaderPlatform(shaderPlatform);

                context.TestProject.GetComponent<CppGlobalCacheImpl>().ResetCache();
            }

            protected override void OnAfterTestExecute(TestAspectContext context)
            {
                var shaderVariantsManager = context.TestProject.GetComponent<ShaderVariantsManager>();
                shaderVariantsManager.ResetAllKeywords();
                shaderVariantsManager.SetShaderApi(ShaderApiDefineSymbolDescriptor.DefaultValue);
                shaderVariantsManager.SetShaderPlatform(ShaderPlatformDefineSymbolDescriptor.DefaultValue);

            }
        }
    }
}