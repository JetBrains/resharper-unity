using JetBrains.ReSharper.FeaturesTestFramework.CodeCleanup;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeStyle.Formatting;
using JetBrains.ReSharper.Psi.CSharp.CodeStyle.FormatSettings;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Psi.CodeStyle
{
    [TestUnity]
    [Category("Formatting"), Category("CSharp")]
    [TestSettingsKey(typeof(UnityCSharpFormattingSettingsKey))]
    [TestSettingsKey(typeof(CSharpFormatSettingsKey))]
    public class CSharpUnityFormatterTest : CodeCleanupTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Psi\CodeStyle\Formatting";

        [Test] public void TestCustomHeaderFormatting() => DoNamedTest();
        [Test] public void TestCustomHeaderBlankLines() => DoNamedTest();
    }
}