#nullable enable

using System;
using System.Linq.Expressions;
using JetBrains.Application.Help;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.Options;
using JetBrains.Application.UI.Options.OptionsDialog;
using JetBrains.Application.UI.Options.OptionsDialog.SimpleOptions;
using JetBrains.Application.UI.Options.OptionsDialog.SimpleOptions.ViewModel;
using JetBrains.DataFlow;
using JetBrains.IDE.UI.Options;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.Rider.Model.UIAutomation;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Application.UI.Options
{
    [OptionsPage(PID, "Profiler Integration",
        typeof(LogoIcons.Unity),
        ParentId = UnityOptionsPage.PID,
        NestingType = OptionPageNestingType.Child,
        HelpKeyword = HelpId.Settings_Unity_Engine_Profiler_Integration)]
    public class UnityProfilerOptionsPage : BeSimpleOptionsPage
    {
        public const string PID = "UnityProfilerOptionsPage";

        public UnityProfilerOptionsPage(Lifetime lifetime,
                                        OptionsPageContext pageContext,
                                        OptionsSettingsSmartContext settingsStore)
            : base(lifetime, pageContext, settingsStore)
        {
            if (!OptionsPageContext.IsRider) return;

            Expression<Func<UnitySettings, bool>> enableAccessor = s => s.EnableProfilerSnapshotFetching;

            AddBoolOption((UnitySettings s) => s.EnableProfilerSnapshotFetching,
                Strings.UnitySettings_Enable_Profiler_Snapshot_Fetching);

            var option = AddComboOption((UnitySettings s) => s.ProfilerSnapshotFetchingMode,
                Strings.UnitySettings_Profiler_Snapshot_Fetching_Mode, string.Empty, string.Empty,
                new RadioOptionPoint(ProfilerSnapshotFetchingMode.Auto, Strings.UnityOptionsPage_Profiler_Integration_Auto_Fetching),
                new RadioOptionPoint(ProfilerSnapshotFetchingMode.Manual, Strings.UnityOptionsPage_Profiler_Integration_Manual_Fetching)
            );
            BindToEnabledProperty(option, enableAccessor);

            option = AddBoolOption((UnitySettings s) => s.IsProfilerGutterMarksDisplayEnabled,
                Strings.UnitySettings_Profiler_New_Highlighting_Enabled);
            BindToEnabledProperty(option, enableAccessor);

            option = AddComboOption((UnitySettings s) => s.ProfilerGutterMarksDisplaySettings,
                Strings.UnityOptionsPage_Profiler_New_Profiler_Highlightings, string.Empty, string.Empty,
                new RadioOptionPoint(ProfilerSnapshotHighlightingSettings.Default, Strings.UnityOptionsPage_Profiler_New_Profiler_Highlightings_Default),
                new RadioOptionPoint(ProfilerSnapshotHighlightingSettings.Minimized, Strings.UnityOptionsPage_Profiler_New_Profiler_Highlightings_Minimized)
            );
            var enableProp = OptionsSettingsSmartContext.GetValueProperty(Lifetime,
                (UnitySettings s) => s.EnableProfilerSnapshotFetching);
            var gutterMarksProp = OptionsSettingsSmartContext.GetValueProperty(Lifetime,
                (UnitySettings s) => s.IsProfilerGutterMarksDisplayEnabled);
            AddBinding(option, BindingStyle.IsEnabledProperty,
                enableProp.And(gutterMarksProp), b => b);
        }

        private void BindToEnabledProperty(BeControl option, Expression<Func<UnitySettings, bool>> setting)
        {
            AddBinding(option, BindingStyle.IsEnabledProperty, setting, enable => enable);
        }
    }
}