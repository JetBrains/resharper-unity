using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using JetBrains.Application.Environment;
using JetBrains.Application.Environment.Helpers;
using JetBrains.Application.UI.Options;
using JetBrains.Application.UI.Options.OptionsDialog;
using JetBrains.Application.UI.Options.OptionsDialog.SimpleOptions;
using JetBrains.Application.UI.Options.OptionsDialog.SimpleOptions.ViewModel;
using JetBrains.IDE.UI.Options;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.OptionPages.CodeEditing;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.Application.UI.Options.VisualStudio
{
    // This is getting very similar to Rider's page
    [OptionsPage(PID, Name, typeof(LogoIcons.Unity), ParentId = CodeEditingPage.PID)]
    public class ReSharperOptionsPage : BeSimpleOptionsPage
    {
        // Note that this is the same as Rider's options page
        public const string PID = "UnityPluginSettings";
        public const string Name = "Unity Engine";

        private static readonly Expression<Func<UnitySettings, bool>> ourEnablePerformanceHighlightingAccessor =
            s => s.EnablePerformanceCriticalCodeHighlighting;
        private static readonly Expression<Func<UnitySettings, bool>> ourEnableBurstHighlightingAccessor =
            s => s.EnableBurstCodeHighlighting;

        public ReSharperOptionsPage(Lifetime lifetime, OptionsPageContext pageContext,
                                    [NotNull] OptionsSettingsSmartContext settingsStore,
                                    RunsProducts.ProductConfigurations productConfigurations)
            : base(lifetime, pageContext, settingsStore)
        {
            AddHeader("C#");
            AddBoolOption(ourEnableBurstHighlightingAccessor, "Enable analysis for Burst compiler issues");

            using (Indent())
            {
                var option = AddBoolOption((UnitySettings s) => s.EnableIconsForBurstCode,
                    "Show icons for Burst compiled called methods");
                AddBinding(option, BindingStyle.IsEnabledProperty, ourEnableBurstHighlightingAccessor,
                    enable => enable);
            }
            
            AddBoolOption(ourEnablePerformanceHighlightingAccessor,
                "Enable performance analysis in frequently called code");

            using (Indent())
            {
                var option = AddComboOption((UnitySettings s) => s.PerformanceHighlightingMode,
                    "Highlight performance critical contexts:", string.Empty, string.Empty,
                    new RadioOptionPoint(PerformanceHighlightingMode.Always, "Always"),
                    new RadioOptionPoint(PerformanceHighlightingMode.CurrentMethod, "Current method only"),
                    new RadioOptionPoint(PerformanceHighlightingMode.Never, "Never")
                );
                AddBinding(option, BindingStyle.IsEnabledProperty, ourEnablePerformanceHighlightingAccessor,
                    enable => enable);
                option = AddBoolOption((UnitySettings s) => s.EnableIconsForPerformanceCriticalCode,
                    "Show icons for frequently called methods");
                AddBinding(option, BindingStyle.IsEnabledProperty, ourEnablePerformanceHighlightingAccessor,
                    enable => enable);
            }

            // The default is when code vision is disabled. Let's keep this so that if/when ReSharper ever gets Code
            // Vision, we'll show the items, or if the user installs Rider, the copied settings will still be useful
            AddBoolOption((UnitySettings s) => s.GutterIconMode,
                GutterIconMode.CodeInsightDisabled, GutterIconMode.None,
                "Show gutter icons for implicit script usages");

            AddHeader("Text based assets");
            AddBoolOption((UnitySettings s) => s.IsAssetIndexingEnabled,
                "Parse text based asset files for implicit script usages (requires re-opening solution)");

            
            AddHeader("Shaders");
            AddBoolOption((UnitySettings s) => s.SuppressShaderErrorHighlighting,
                "Suppress resolve errors of unqualified names");
            
            if (productConfigurations.IsInternalMode())
            {
                AddHeader("Internal");

                AddBoolOption((UnitySettings s) => s.SuppressShaderErrorHighlightingInRenderPipelinePackages, 
                    "Suppress resolve errors in render-pipeline packages");
                
                AddBoolOption((UnitySettings s) => s.EnableCgErrorHighlighting,
                    "[Deprecated] Parse GLSL files for syntax errors (requires internal mode, and re-opening solution)");
            }
        }
    }
}