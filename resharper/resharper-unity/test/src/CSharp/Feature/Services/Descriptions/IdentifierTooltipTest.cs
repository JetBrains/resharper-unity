using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Feature.Services.Descriptions
{
    [TestUnity]
    // The tests include tooltips from all highlighters at the caret position, which will include gutter icons for
    // ReSharper, but not Rider as we prefer Code Vision. Force gutter icons on to avoid splitting the test per product.
    [TestSetting(typeof(UnitySettings), nameof(UnitySettings.GutterIconMode), GutterIconMode.Always)]
    public class IdentifierTooltipTest : IdentifierTooltipTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\IdentifierTooltip";

        // TODO: ReSharper currently ignores this setting, and always shows gutter icons
#if RIDER
        [Test, TestSetting(typeof(UnitySettings), nameof(UnitySettings.GutterIconMode), GutterIconMode.None)]
        public void EventFunction01() { DoNamedTest(); }
        [Test, TestSetting(typeof(UnitySettings), nameof(UnitySettings.GutterIconMode), GutterIconMode.None)]
        public void EventFunction02() { DoNamedTest(); }
#endif
        [Test] public void EventFunctionWithGutterIcon01() { DoNamedTest(); }
        [Test] public void EventFunctionWithGutterIcon02() { DoNamedTest(); }
        [Test] public void EventFunctionParameter01() { DoNamedTest(); }
        [Test] public void EventFunctionParameter02() { DoNamedTest(); }
        [Test] public void SerialisedField() { DoNamedTest(); }
        [Test] public void SerialisedFieldWithTooltip() { DoNamedTest(); }
        [Test] public void SerialisedFieldWithTooltipIgnoresXmlDoc() { DoNamedTest(); }
    }
}