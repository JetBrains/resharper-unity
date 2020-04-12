using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Intentions.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.ShaderLab.Psi.Colors
{
    [Category("ColorHighlighting")]
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabColorPickerTests : QuickFixTestBase<ColorPickerQuickFix>
    {
        protected override string RelativeTestDataPath => @"ShaderLab\Intentions\QuickFixes\ColorPicker";

        [SetCulture("en-US")] [Test] public void TestChangeColorCultureEn() { DoOneTest("ChangeColor"); }
        [SetCulture("de-DE")] [Test] public void TestChangeColorCultureDe() { DoOneTest("ChangeColor"); }
    }
}