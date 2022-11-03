using System;
using System.Linq.Expressions;
using JetBrains.Application.Environment;
using JetBrains.Application.Environment.Helpers;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.Options;
using JetBrains.Application.UI.Options.OptionsDialog;
using JetBrains.Application.UI.Options.OptionsDialog.SimpleOptions;
using JetBrains.Application.UI.Options.OptionsDialog.SimpleOptions.ViewModel;
using JetBrains.DataFlow;
using JetBrains.Diagnostics;
using JetBrains.IDE.UI.Extensions;
using JetBrains.IDE.UI.Options;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.OptionPages.CodeEditing;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Naming.Elements;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Psi.CSharp.Naming2;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.Rider.Model.UIAutomation;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Application.UI.Options
{
    [OptionsPage(PID, Name, typeof(LogoIcons.Unity), ParentId = CodeEditingPage.PID)]
    public class UnityOptionsPage : BeSimpleOptionsPage
    {
        public const string PID = "UnityPluginSettings";
        public const string Name = "Unity Engine";

        private static readonly Expression<Func<CSharpNamingSettings, IIndexedEntry<Guid, ClrUserDefinedNamingRule>>>
            ourUserRulesAccessor = s => s.UserRules;

        private static readonly Expression<Func<UnitySettings, bool>> ourEnablePerformanceHighlightingAccessor =
            s => s.EnablePerformanceCriticalCodeHighlighting;
        private static readonly Expression<Func<UnitySettings, bool>> ourEnableBurstHighlightingAccessor =
            s => s.EnableBurstCodeHighlighting;

        public UnityOptionsPage(Lifetime lifetime,
                                OptionsPageContext pageContext,
                                OptionsSettingsSmartContext settingsStore,
                                RunsProducts.ProductConfigurations productConfigurations)
            : base(lifetime, pageContext, settingsStore)
        {
            AddGeneralSection();
            AddCSharpSection();
            AddTextBasedAssetsSection();
            AddShadersSection();
            AddDebuggingSection();
            AddInternalSection(productConfigurations);
        }

        private void AddGeneralSection()
        {
            if (!OptionsPageContext.IsRider) return;

            AddHeader(Strings.UnityOptionsPage_AddGeneralSection_General);
            AddBoolOption((UnitySettings s) => s.InstallUnity3DRiderPlugin,
                Strings.UnityOptionsPage_AddGeneralSection_Automatically_install_and_update_Rider_s_Unity_editor_plugin);
            AddBetterCommentText(Strings.UnityOptionsPage_AddGeneralSection_);

            AddBoolOption((UnitySettings s) => s.AllowAutomaticRefreshInUnity,
                Strings.UnityOptionsPage_AddGeneralSection_Automatically_refresh_assets_in_Unity);
            
            AddBoolOption((UnitySettings s) => s.AllowRiderUpdateNotifications,
                Strings.UnityOptionsPage_AddGeneralSection_Notify_when_Rider_package_update_is_available);
        }

        private void AddCSharpSection()
        {
            AddHeader("C#");

            // Show simplified text box for ReSharper, while Rider has a drop down. Note that the unchecked value for
            // ReSharper is "when Code Vision is disabled". If/when R# gets Code Vision, the settings will be good
            if (OptionsPageContext.IsRider)
            {
                AddComboOption((UnitySettings s) => s.GutterIconMode,
                    Strings.UnityOptionsPage_AddCSharpSection_Show_gutter_icons_for_implicit_script_usages_, string.Empty, string.Empty,
                    new RadioOptionPoint(GutterIconMode.Always, Strings.UnityOptionsPage_AddCSharpSection_Always),
                    new RadioOptionPoint(GutterIconMode.CodeInsightDisabled, Strings.UnityOptionsPage_AddCSharpSection_When_Code_Vision_is_disabled),
                    new RadioOptionPoint(GutterIconMode.None, Strings.UnityOptionsPage_AddPerformanceAnalysisSubSection_Never)
                );
            }
            else
            {
                AddBoolOption((UnitySettings s) => s.GutterIconMode,
                    GutterIconMode.CodeInsightDisabled, GutterIconMode.None,
                    Strings.UnityOptionsPage_AddCSharpSection_Show_gutter_icons_for_implicit_script_usages);
            }

            AddPerformanceAnalysisSubSection();
            AddBurstAnalysisSubSection();
            using (Indent())
                AddNamingSubSection();
        }

        private void AddPerformanceAnalysisSubSection()
        {
            AddBoolOption(ourEnablePerformanceHighlightingAccessor,
                Strings.UnityOptionsPage_AddPerformanceAnalysisSubSection_Enable_performance_analysis_in_frequently_called_code);

            using (Indent())
            {
                var option = AddComboOption((UnitySettings s) => s.PerformanceHighlightingMode,
                    Strings.UnityOptionsPage_AddPerformanceAnalysisSubSection_Highlight_performance_critical_contexts_, string.Empty, string.Empty,
                    new RadioOptionPoint(PerformanceHighlightingMode.Always, Strings.UnityOptionsPage_AddCSharpSection_Always),
                    new RadioOptionPoint(PerformanceHighlightingMode.CurrentMethod, Strings.UnityOptionsPage_AddPerformanceAnalysisSubSection_Current_method_only),
                    new RadioOptionPoint(PerformanceHighlightingMode.Never, Strings.UnityOptionsPage_AddPerformanceAnalysisSubSection_Never)
                );
                BindToEnabledProperty(option, ourEnablePerformanceHighlightingAccessor);
                option = AddBoolOption((UnitySettings s) => s.EnableIconsForPerformanceCriticalCode,
                    Strings.UnityOptionsPage_AddPerformanceAnalysisSubSection_Show_gutter_icons_for_frequently_called_methods);
                BindToEnabledProperty(option, ourEnablePerformanceHighlightingAccessor);
            }
        }

        private void AddBurstAnalysisSubSection()
        {
            AddBoolOption(ourEnableBurstHighlightingAccessor, Strings.UnityOptionsPage_AddBurstAnalysisSubSection_Enable_analysis_for_Burst_compiler_issues);

            using (Indent())
            {
                var option = AddBoolOption((UnitySettings s) => s.EnableIconsForBurstCode,
                    Strings.UnityOptionsPage_AddBurstAnalysisSubSection_Show_gutter_icons_for_Burst_compiled_called_methods);
                BindToEnabledProperty(option, ourEnableBurstHighlightingAccessor);
            }
        }

        private void AddNamingSubSection()
        {
            // ReSharper already has a UI for editing user defined rules. Rider doesn't. See RIDER-8339
            if (!OptionsPageContext.IsRider) return;

            AddHeader(Strings.UnityOptionsPage_AddNamingSubSection_Serialized_field_naming_rules);

            var entry = OptionsSettingsSmartContext.Schema.GetIndexedEntry(ourUserRulesAccessor);
            var userRule = GetUnitySerializedFieldRule(OptionsSettingsSmartContext, entry);
            Assertion.AssertNotNull(userRule, "userRule != null");

            var prefixProperty = new Property<string>(Lifetime, "StringOptionViewModel_SerializedFieldPrefix");
            prefixProperty.SetValue(userRule.Policy.NamingRule.Prefix);
            prefixProperty.Change.Advise_NoAcknowledgement(Lifetime, args =>
            {
                var rule = GetUnitySerializedFieldRule(OptionsSettingsSmartContext, entry);
                rule.Policy.NamingRule.Prefix = args.New ?? string.Empty;
                SetUnitySerializedFieldRule(OptionsSettingsSmartContext, entry, rule);
            });

            var suffixProperty = new Property<string>(Lifetime, "StringOptionViewModel_SerializedFieldSuffix");
            suffixProperty.SetValue(userRule.Policy.NamingRule.Suffix);
            suffixProperty.Change.Advise_NoAcknowledgement(Lifetime, args =>
            {
                var rule = GetUnitySerializedFieldRule(OptionsSettingsSmartContext, entry);
                rule.Policy.NamingRule.Suffix = args.New ?? string.Empty;
                SetUnitySerializedFieldRule(OptionsSettingsSmartContext, entry, rule);
            });

            var kindProperty = new Property<object>(Lifetime, "ComboOptionViewModel_SerializedFieldNamingStyle");
            kindProperty.SetValue(userRule.Policy.NamingRule.NamingStyleKind);
            kindProperty.Change.Advise_NoAcknowledgement(Lifetime, args =>
            {
                var rule = GetUnitySerializedFieldRule(OptionsSettingsSmartContext, entry);
                rule.Policy.NamingRule.NamingStyleKind = (NamingStyleKinds)args.New;
                SetUnitySerializedFieldRule(OptionsSettingsSmartContext, entry, rule);
            });

            var enabledProperty =
                new Property<bool>(Lifetime, "BoolOptionViewModel_SerializedFieldEnableInspection");
            enabledProperty.SetValue(userRule.Policy.EnableInspection);
            enabledProperty.Change.Advise_NoAcknowledgement(Lifetime, args =>
            {
                var existingRule = GetUnitySerializedFieldRule(OptionsSettingsSmartContext, entry);
                var newRule = new ClrUserDefinedNamingRule(existingRule.Descriptor,
                    new NamingPolicy(existingRule.Policy.ExtraRules.ToIReadOnlyList(),
                        existingRule.Policy.NamingRule, args.New));
                SetUnitySerializedFieldRule(OptionsSettingsSmartContext, entry, newRule);
            });

            AddStringOption(Lifetime, prefixProperty, Strings.UnityOptionsPage_AddNamingSubSection_Prefix_);
            AddStringOption(Lifetime, suffixProperty, Strings.UnityOptionsPage_AddNamingSubSection_Suffix_);
            AddComboOption(kindProperty, Strings.UnityOptionsPage_AddNamingSubSection_Style_, string.Empty, string.Empty,
                new[]
                {
                    new RadioOptionPoint(NamingStyleKinds.AaBb, Strings.UnityOptionsPage_AddNamingSubSection_UpperCamelCase),
                    new RadioOptionPoint(NamingStyleKinds.AaBb_AaBb, Strings.UnityOptionsPage_AddNamingSubSection_UpperCamelCase_UnderscoreTolerant),
                    new RadioOptionPoint(NamingStyleKinds.AaBb_aaBb, Strings.UnityOptionsPage_AddNamingSubSection_UpperCamelCase_underscoreTolerant2),
                    new RadioOptionPoint(NamingStyleKinds.aaBb, Strings.UnityOptionsPage_AddNamingSubSection_lowerCamelCase),
                    new RadioOptionPoint(NamingStyleKinds.aaBb_AaBb, Strings.UnityOptionsPage_AddNamingSubSection_lowerCamelCase_UnderscoreTolerant),
                    new RadioOptionPoint(NamingStyleKinds.aaBb_aaBb, Strings.UnityOptionsPage_AddNamingSubSection_lowerCamelCase_underscoreTolerant2),
                    new RadioOptionPoint(NamingStyleKinds.AA_BB, Strings.UnityOptionsPage_AddNamingSubSection_ALL_UPPER),
                    new RadioOptionPoint(NamingStyleKinds.Aa_bb, Strings.UnityOptionsPage_AddNamingSubSection_First_upper)
                });
            AddBoolOption(enabledProperty, Strings.UnityOptionsPage_AddNamingSubSection_Enable_inspection, null);
        }

        private static ClrUserDefinedNamingRule GetUnitySerializedFieldRule(IContextBoundSettingsStore settingsStore,
                                                                            SettingsIndexedEntry entry)
        {
            var userRule = settingsStore.GetIndexedValue(entry,
                UnityNamingRuleDefaultSettings.SerializedFieldRuleGuid, null) as ClrUserDefinedNamingRule;
            if (userRule == null)
            {
                userRule = UnityNamingRuleDefaultSettings.GetUnitySerializedFieldRule();
                SetUnitySerializedFieldRule(settingsStore, entry, userRule);
            }

            return userRule;
        }

        private static void SetUnitySerializedFieldRule(IContextBoundSettingsStore settingsStore,
                                                        SettingsIndexedEntry entry, ClrUserDefinedNamingRule userRule)
        {
            settingsStore.SetIndexedValue(entry, UnityNamingRuleDefaultSettings.SerializedFieldRuleGuid, null,
                userRule);
        }

        private void AddTextBasedAssetsSection()
        {
            AddHeader(Strings.UnityOptionsPage_AddTextBasedAssetsSection_Text_based_assets);
            AddBoolOption((UnitySettings s) => s.IsAssetIndexingEnabled,
                Strings.UnityOptionsPage_AddTextBasedAssetsSection_Parse_text_based_asset_files_for_script_and_event_handler_usages);

            if (OptionsPageContext.IsRider)
            {
                AddBoolOption((UnitySettings s) => s.EnableInspectorPropertiesEditor,
                    Strings.UnityOptionsPage_AddTextBasedAssetsSection_Show_Inspector_values_in_the_editor);
            }

            AddBoolOption((UnitySettings s) => s.IsPrefabCacheEnabled,
                Strings.UnityOptionsPage_AddTextBasedAssetsSection_Cache_prefab_data_to_improve_find_usage_performance);
            AddBoolOption((UnitySettings s) => s.EnableAssetIndexingPerformanceHeuristic,
                Strings.UnityOptionsPage_AddTextBasedAssetsSection_Automatically_disable_asset_indexing_for_large_solutions);

            if (OptionsPageContext.IsRider)
            {
                AddBoolOption((UnitySettings s) => s.UseUnityYamlMerge, Strings.UnityOptionsPage_AddTextBasedAssetsSection_Prefer_UnityYamlMerge_for_merging_YAML_files);
                using (Indent())
                {
                    var option = AddControl((UnitySettings s) => s.MergeParameters,
                        p => p.GetBeTextBox(Lifetime).WithDescription(Strings.UnityOptionsPage_AddTextBasedAssetsSection_Merge_parameters, Lifetime));
                    BindToEnabledProperty(option, s => s.UseUnityYamlMerge);
                }
            }
        }

        private void AddShadersSection()
        {
            // TODO: For ReSharper, this is unavailable if the user hasn't installed ReSharper C++
            AddHeader(Strings.UnityOptionsPage_AddShadersSection_Shaders);
            AddBoolOption((UnitySettings s) => s.SuppressShaderErrorHighlighting,
                Strings.UnityOptionsPage_AddShadersSection_Suppress_resolve_errors_of_unqualified_names);
        }

        private void AddDebuggingSection()
        {
            if (!OptionsPageContext.IsRider) return;

            AddHeader(Strings.UnityOptionsPage_AddDebuggingSection_Debugging);
            AddBoolOption((UnitySettings s) => s.EnableDebuggerExtensions,
                Strings.UnityOptionsPage_AddDebuggingSection_Extend_value_rendering);
            AddBetterCommentText(Strings.UnityOptionsPage_AddDebuggingSection_Extend_value_rendering_Comment);

            AddBoolOption((UnitySettings s) => s.IgnoreBreakOnUnhandledExceptionsForIl2Cpp,
                Strings.UnityOptionsPage_AddDebuggingSection_Ignore__Break_on_unhandled_exceptions__setting_for_IL2CPP_players);
            AddBetterCommentText(Strings.UnityOptionsPage_AddDebuggingSection_Break_on_unhandled_exceptions__setting_for_IL2CPP_players_Comment);
        }

        private void AddInternalSection(RunsProducts.ProductConfigurations productConfigurations)
        {
            if (!productConfigurations.IsInternalMode()) return;

            AddHeader(Strings.UnityOptionsPage_AddInternalSection_Internal);

            AddBoolOption((UnitySettings s) => s.SuppressShaderErrorHighlightingInRenderPipelinePackages,
                Strings.UnityOptionsPage_AddInternalSection_Suppress_resolve_errors_in_render_pipeline_packages);

            AddBoolOption((UnitySettings s) => s.EnableCgErrorHighlighting,
                Strings.UnityOptionsPage_AddInternalSection__Deprecated__Parse_GLSL_files_for_syntax_errors__requires_internal_mode__and_re_opening_solution_);
        }

        private void AddStringOption(Lifetime lifetime, IProperty<string> property, string text)
        {
            AddControlWithProperty(property, p => p.GetBeTextBox(lifetime).WithDescription(text, lifetime));
        }

        private void AddBetterCommentText(string text)
        {
            // AddCommentText doesn't match the UI guidelines for inline help. It doesn't indent, uses the wrong theme
            // colour, should wrap at about 70 characters and have a slightly smaller font size.
            // https://youtrack.jetbrains.com/issue/RIDER-47090
            using (Indent())
            {
                var comment = CreateCommentText(text).WithCustomTextSize(BeFontSize.SMALLER);
                AddControl(comment);
            }

            AddKeyword(text);
        }

        private void BindToEnabledProperty(BeControl option, Expression<Func<UnitySettings, bool>> setting)
        {
            AddBinding(option, BindingStyle.IsEnabledProperty, setting, enable => enable);
        }
    }
}