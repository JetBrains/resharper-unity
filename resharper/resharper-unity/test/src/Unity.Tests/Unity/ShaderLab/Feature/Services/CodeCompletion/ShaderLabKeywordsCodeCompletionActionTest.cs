using JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings;
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Formatting;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Feature.Services.CodeCompletion
{
    [RequireHlslSupport]
    [TestSetting(typeof(CodeCompletionSettingsKey), nameof(CodeCompletionSettingsKey.ReplaceKeywordsWithTemplates), false)]
    [TestSettingsKey(typeof(ShaderLabFormatSettingsKey))]
    public class ShaderLabKeywordsCodeCompletionActionTest : ShaderLabCodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.Action;

        protected override bool CheckAutomaticCompletionDefault() => true;

        protected override string RelativeTestDataPath => @"ShaderLab\CodeCompletion\Keywords";
        
        [Test] public void TestKeywordReplacement() => DoNamedTest();
        
        [Test] public void TestBlockCommandCompletion01() => DoNamedTest();
        
        [Test] public void TestBlockCommandCompletion02() => DoNamedTest();
    }
}