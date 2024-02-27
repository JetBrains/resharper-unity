using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Tests.Unity;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.Shaders.HlslSupport.Daemon.CodeInsights;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Settings;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Tests.UnityRider.ShaderLab.Daemon.CodeInsights;

[TestUnity, HighlightOnly(typeof(InactiveShaderVariantBranchHighlight))]
[TestSetting(typeof(UnitySettings), nameof(UnitySettings.FeaturePreviewShaderVariantsSupport), true)]
[TestIndexedSetting(typeof(ShaderVariantsSettings), nameof(ShaderVariantsSettings.EnabledKeywords), "B", true, null)]
[TestIndexedSetting(typeof(ShaderVariantsSettings), nameof(ShaderVariantsSettings.EnabledKeywords), "F", true, null)]
[TestFileExtension(".shader")]
public class InactiveShaderVariantBranchCodeInsightsTests : HighlightingTestBase
{
    protected override string RelativeTestDataPath => @"ShaderLab\Daemon\CodeInsights";
    
    [TestCase]
    public void TestShaderVariantBranches() => DoNamedTest2();

    protected override PsiLanguageType? CompilerIdsLanguage => CppLanguage.Instance;
}