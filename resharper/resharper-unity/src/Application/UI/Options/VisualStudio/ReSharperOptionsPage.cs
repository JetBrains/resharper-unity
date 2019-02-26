using JetBrains.Annotations;
using JetBrains.Application.Environment;
using JetBrains.Application.Environment.Helpers;
using JetBrains.Application.UI.Options;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.OptionPages.CodeEditing;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.Application.UI.Options.VisualStudio
{
    // TODO: Merge with Rider's page?
    // That would leave us with #ifdef soup...
    [OptionsPage(PID, Name, typeof(LogoThemedIcons.UnityLogo), ParentId = CodeEditingPage.PID)]
    public class ReSharperOptionsPage : OptionsPageBase
    {
        // Note that this is the same as Rider's options page
        public const string PID = "UnityPluginSettings";
        public const string Name = "Unity Engine";

        public ReSharperOptionsPage(Lifetime lifetime, [NotNull] OptionsSettingsSmartContext settingsStore,
            RunsProducts.ProductConfigurations productConfigurations)
            : base(lifetime, settingsStore)
        {
            Header("General");

            CheckBox((UnitySettings s) => s.IsYamlParsingEnabled,
                "Parse text based asset files for implicit script usages");

            Header("C#");
            CheckBox((UnitySettings s) => s.EnablePerformanceCriticalCodeHighlighting,
                "Highlight expensive method calls in frequently called code");

            // The default is when code vision is disabled. Let's keep this so that if/when ReSharper ever gets Code
            // Vision, we'll show the items, or if the user installs Rider, the copied settings will still be useful
            AddBoolOption((UnitySettings s) => s.GutterIconMode,
                GutterIconMode.CodeInsightDisabled, GutterIconMode.None,
                "Show gutter icons for implicit script usages:");

            if (productConfigurations.IsInternalMode())
            {
                Header("Internal");

                CheckBox((UnitySettings s) => s.EnableCgErrorHighlighting,
                    "Parse Cg files for syntax errors (requires internal mode, and re-opening solution)");
            }

            FinishPage();
        }
    }
}