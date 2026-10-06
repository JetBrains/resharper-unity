using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Intentions.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [Category("ColorHighlighting")]
    [TestUnity]
    public class ColorPickerTest : CSharpQuickFixTestBase<ColorPickerQuickFix>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\ColorPicker";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Colors", "Change color value")] public void TestChangeToNamedColor() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Colors", "Change color value")] public void TestChangeToNamedColor2() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Colors", "Change color value")] public void TestChangeToColorConstructor() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Colors", "Change color value")] public void TestChangeToColorConstructor2() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Colors", "Change color value")] public void TestChangeToColorConstructor3() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Colors", "Change color value")] public void TestChangeToColorConstructor4() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Colors", "Change color value")] public void TestChangeExistingHSV() { DoNamedTest2(); }

        [Test, UnityPluginBackendChecklist("Quick fixes", "Colors", "Change color value")] public void TestChangeToColor32Constructor() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Colors", "Change color value")] public void TestChangeToColor32Constructor2() { DoNamedTest2(); }
    }
}