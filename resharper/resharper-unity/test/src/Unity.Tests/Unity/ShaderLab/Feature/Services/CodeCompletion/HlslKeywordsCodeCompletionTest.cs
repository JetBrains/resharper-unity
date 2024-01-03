using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Settings;
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Feature.Services.CodeCompletion
{
    [TestUnity, RequireHlslSupport]
    [CgIncludesDirectory("ShaderLab/CGIncludes")]
    [TestSetting(typeof(CodeCompletionSettingsKey), nameof(CodeCompletionSettingsKey.ReplaceKeywordsWithTemplates), false)]
    public class HlslKeywordsCodeCompletionTest : ShaderLabCodeCompletionTestBase
    {
        protected override CodeCompletionTestType TestType => CodeCompletionTestType.List;

        protected override bool CheckAutomaticCompletionDefault() => true;

        protected override string RelativeTestDataPath => @"ShaderLab\CodeCompletion\Hlsl\List";

        [TestCase("CgKeywords", ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
        [TestCase("HlslKeywords", ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
        [TestCase("HlslKeywords", CppProjectFileType.HLSL_EXTENSION)]
        [TestCase("CgTrivialReparseContext", ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
        public void TestCompletion(string testName, string extension) => DoTestSolution(testName + extension);
    }
}