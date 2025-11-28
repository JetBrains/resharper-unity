using System;
using System.Text.RegularExpressions;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.Caches;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.ShaderVariants;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Model;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi;
using JetBrains.TextControl;
using JetBrains.Util;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Intentions.QuickFixes;

[RequireHlslSupport, TestUnity]
public class ShaderKeywordQuickFixTest : QuickFixTestBase<ShaderKeywordQuickFix>
{
    private static readonly Regex ourParamRegex = new(
        "^\\$\\$ (?<Name>\\w+)\\s*:\\s*(?<Value>.*)", RegexOptions.Multiline);

    protected override string RelativeTestDataPath=> @"ShaderLab\Intentions\QuickFixes\ShaderKeyword";

    protected override bool DumpBulbText => true;

    [TestCase("Test01.shader")]
    [TestCase("Test02.shader")]
    [TestCase("Test03.shader")]
    [TestCase("Test04.shader")]
    [TestCase("Test05.shader")]
    [TestCase("Test06.shader")]
    public void Test(string fileName) => DoTestSolution(fileName);

    protected override void DoTestOnTextControlAndExecuteWithGold(
        IProject testProject, ITextControl textControl, IPsiSourceFile sourceFile)
    {
        var shaderVariantsManager = testProject.GetComponent<ShaderVariantsManager>();
        ShaderApi? expectedShaderApi = null;
        var expectedEnabledKeywords = new LocalList<string>();
        var expectedDisabledKeywords = new LocalList<string>();

        foreach (Match match in ourParamRegex.Matches(textControl.Document.GetText()))
        {
            var name = match.Groups["Name"].Value;
            var value = match.Groups["Value"].Value;
            switch (name)
            {
                case "CheckShaderApi":
                    expectedShaderApi = (ShaderApi)Enum.Parse(typeof(ShaderApi), value);
                    break;
                case "CheckShaderKeywordEnabled":
                    expectedEnabledKeywords.Add(value);
                    break;
                case "CheckShaderKeywordDisabled":
                    expectedDisabledKeywords.Add(value);
                    break;
                case "EnableShaderKeyword":
                    shaderVariantsManager.SetKeywordEnabled(value, true);
                    break;
                case "DisableShaderKeyword":
                    shaderVariantsManager.SetKeywordEnabled(value, false);
                    break;
                default:
                    throw new NotSupportedException($"Not supported test option: {name}");
            }
        }

        testProject.GetComponent<CppGlobalCacheImpl>().ResetCache();

        base.DoTestOnTextControlAndExecuteWithGold(testProject, textControl, sourceFile);

        if (expectedShaderApi.HasValue)
            Assert.That(shaderVariantsManager.ShaderApi, Is.EqualTo(expectedShaderApi.Value));

        foreach (var keyword in expectedEnabledKeywords)
            Assert.That(shaderVariantsManager.IsKeywordEnabled(keyword), Is.True);
        foreach (var keyword in expectedDisabledKeywords)
            Assert.That(shaderVariantsManager.IsKeywordEnabled(keyword), Is.False);

        shaderVariantsManager.ResetAllKeywords();
        shaderVariantsManager.SetShaderApi(ShaderApiDefineSymbolDescriptor.DefaultValue);
    }
}