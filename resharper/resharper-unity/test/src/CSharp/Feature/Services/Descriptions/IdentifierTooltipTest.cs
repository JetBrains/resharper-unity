using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Feature.Services.Descriptions
{
    // TODO: WHY DOES TESTSETTING NOT WORK!??!
    [TestUnity]
    // The tests include tooltips from all highlighters at the caret position, which will include gutter icons for
    // ReSharper, but not Rider as we prefer Code Vision. Force gutter icons on to avoid splitting the test per product.
    // TODO: Include tests for this setting always on, and always off
    // ReSharper currently ignores this option, and I can't get [TestSetting] to work correctly...
    // [TestSetting(typeof(UnitySettings), nameof(UnitySettings.GutterIconMode), GutterIconMode.Always)]
    public class IdentifierTooltipTest : IdentifierTooltipTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\IdentifierTooltip";

        // TODO: TestSetting is ignored if set again. It's called, but the component doesn't see the changed value
        // [Test, TestSetting(typeof(UnitySettings), nameof(UnitySettings.GutterIconMode), GutterIconMode.None)]
        // public void EventFunction01() { DoNamedTest(); }
        // [Test, TestSetting(typeof(UnitySettings), nameof(UnitySettings.GutterIconMode), GutterIconMode.None)]
        // public void EventFunction02() { DoNamedTest(); }
#if RIDER
        [Test] public void EventFunction01() { DoNamedTest(); }
        [Test] public void EventFunction02() { DoNamedTest(); }
#else
        [Test] public void EventFunctionWithGutterIcon01() { DoNamedTest(); }
        [Test] public void EventFunctionWithGutterIcon02() { DoNamedTest(); }
#endif
        [Test] public void EventFunctionParameter01() { DoNamedTest(); }
        [Test] public void EventFunctionParameter02() { DoNamedTest(); }
    }
}