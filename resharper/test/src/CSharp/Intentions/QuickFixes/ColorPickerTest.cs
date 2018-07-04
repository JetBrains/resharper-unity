using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Intentions.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.QuickFixes
{
    [Category("ColorHighlighting")]
    [TestUnity]
    public class ColorPickerTest : CSharpQuickFixTestBase<ColorPickerQuickFix>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\ColorPicker";

        [Test] public void TestChangeToNamedColor() { DoNamedTest2(); }
        [Test] public void TestChangeToColorConstructor() { DoNamedTest2(); }
        [Test] public void TestChangeToColorConstructor2() { DoNamedTest2(); }
        [Test] public void TestChangeToColorConstructor3() { DoNamedTest2(); }
        [Test] public void TestChangeExistingHSV() { DoNamedTest2(); }

        [Test] public void TestChangeToColor32Constructor() { DoNamedTest2(); }
        [Test] public void TestChangeToColor32Constructor2() { DoNamedTest2(); }
    }
}