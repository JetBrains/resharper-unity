using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using JetBrains.Application.Environment;
using JetBrains.Application.Environment.Helpers;
using JetBrains.Application.UI.Options;
using JetBrains.Application.UI.Options.OptionsDialog.SimpleOptions;
using JetBrains.Application.UI.Options.OptionsDialog.SimpleOptions.ViewModel;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.OptionPages.CodeEditing;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.Application.UI.Options.VisualStudio
{
    // This is getting very similar to Rider's page
    [OptionsPage(PID, Name, typeof(LogoThemedIcons.UnityLogo), ParentId = CodeEditingPage.PID)]
    public class ReSharperOptionsPage : OptionsPageBase
    {
        // Note that this is the same as Rider's options page
        public const string PID = "UnityPluginSettings";
        public const string Name = "Unity Engine";

        private static readonly Expression<Func<UnitySettings, bool>> ourEnablePerformanceHighlightingAccessor =
            s => s.EnablePerformanceCriticalCodeHighlighting;

        public ReSharperOptionsPage(Lifetime lifetime, [NotNull] OptionsSettingsSmartContext settingsStore,
                                    RunsProducts.ProductConfigurations productConfigurations)
            : base(lifetime, settingsStore)
        {
            Header("C#");
            CheckBox((UnitySettings s) => s.EnablePerformanceCriticalCodeHighlighting,
                "Enable performance analysis in frequently called code");

            BeginSection();
            {
                var option = WithIndent(AddComboOption((UnitySettings s) => s.PerformanceHighlightingMode,
                    "Highlight performance critical contexts:",
                    new RadioOptionPoint(PerformanceHighlightingMode.Always, "Always"),
                    new RadioOptionPoint(PerformanceHighlightingMode.CurrentMethod, "Current method only"),
                    new RadioOptionPoint(PerformanceHighlightingMode.Never, "Never")
                ));
                AddBinding(option, BindingStyle.IsEnabledProperty, ourEnablePerformanceHighlightingAccessor,
                    enable => enable);
                option = WithIndent(CheckBox((UnitySettings s) => s.EnableIconsForPerformanceCriticalCode,
                    "Show icons for frequently called methods"));
                AddBinding(option, BindingStyle.IsEnabledProperty, ourEnablePerformanceHighlightingAccessor,
                    enable => enable);
            }
            EndSection();

            // The default is when code vision is disabled. Let's keep this so that if/when ReSharper ever gets Code
            // Vision, we'll show the items, or if the user installs Rider, the copied settings will still be useful
            AddBoolOption((UnitySettings s) => s.GutterIconMode,
                GutterIconMode.CodeInsightDisabled, GutterIconMode.None,
                "Show gutter icons for implicit script usages");

            Header("Text based assets");
            CheckBox((UnitySettings s) => s.IsAssetIndexingEnabled,
                "Parse text based asset files for implicit script usages (requires re-opening solution)");

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