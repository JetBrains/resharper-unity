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

            AddHeader("General");
            AddBoolOption((UnitySettings s) => s.InstallUnity3DRiderPlugin,
                "Automatically install and update Rider's Unity editor plugin");
            AddBetterCommentText("Recommended. Integration features such as play/pause, log view and\n" +
                                 "refreshing assets in the background are automatically supported by\n" +
                                 "the Rider package in Unity 2019.2 and newer. Earlier versions require\n" +
                                 "a plugin to be installed to a project.");

            AddBoolOption((UnitySettings s) => s.AllowAutomaticRefreshInUnity,
                "Automatically refresh assets in Unity");
        }

        private void AddCSharpSection()
        {
            AddHeader("C#");

            // Show simplified text box for ReSharper, while Rider has a drop down. Note that the unchecked value for
            // ReSharper is "when Code Vision is disabled". If/when R# gets Code Vision, the settings will be good
            if (OptionsPageContext.IsRider)
            {
                AddComboOption((UnitySettings s) => s.GutterIconMode,
                    "Show gutter icons for implicit script usages:", string.Empty, string.Empty,
                    new RadioOptionPoint(GutterIconMode.Always, "Always"),
                    new RadioOptionPoint(GutterIconMode.CodeInsightDisabled, "When Code Vision is disabled"),
                    new RadioOptionPoint(GutterIconMode.None, "Never")
                );
            }
            else
            {
                AddBoolOption((UnitySettings s) => s.GutterIconMode,
                    GutterIconMode.CodeInsightDisabled, GutterIconMode.None,
                    "Show gutter icons for implicit script usages");
            }

            AddPerformanceAnalysisSubSection();
            AddBurstAnalysisSubSection();
            using (Indent())
                AddNamingSubSection();
        }

        private void AddPerformanceAnalysisSubSection()
        {
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
                BindToEnabledProperty(option, ourEnablePerformanceHighlightingAccessor);
                option = AddBoolOption((UnitySettings s) => s.EnableIconsForPerformanceCriticalCode,
                    "Show gutter icons for frequently called methods");
                BindToEnabledProperty(option, ourEnablePerformanceHighlightingAccessor);
            }
        }

        private void AddBurstAnalysisSubSection()
        {
            AddBoolOption(ourEnableBurstHighlightingAccessor, "Enable analysis for Burst compiler issues");

            using (Indent())
            {
                var option = AddBoolOption((UnitySettings s) => s.EnableIconsForBurstCode,
                    "Show gutter icons for Burst compiled called methods");
                BindToEnabledProperty(option, ourEnableBurstHighlightingAccessor);
            }
        }

        private void AddNamingSubSection()
        {
            // ReSharper already has a UI for editing user defined rules. Rider doesn't. See RIDER-8339
            if (!OptionsPageContext.IsRider) return;

            AddHeader("Serialized field naming rules");

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

            AddStringOption(Lifetime, prefixProperty, "Prefix:");
            AddStringOption(Lifetime, suffixProperty, "Suffix:");
            AddComboOption(kindProperty, "Style:", string.Empty, string.Empty,
                new[]
                {
                    new RadioOptionPoint(NamingStyleKinds.AaBb, "UpperCamelCase"),
                    new RadioOptionPoint(NamingStyleKinds.AaBb_AaBb, "UpperCamelCase_UnderscoreTolerant"),
                    new RadioOptionPoint(NamingStyleKinds.AaBb_aaBb, "UpperCamelCase_underscoreTolerant"),
                    new RadioOptionPoint(NamingStyleKinds.aaBb, "lowerCamelCase"),
                    new RadioOptionPoint(NamingStyleKinds.aaBb_AaBb, "lowerCamelCase_UnderscoreTolerant"),
                    new RadioOptionPoint(NamingStyleKinds.aaBb_aaBb, "lowerCamelCase_underscoreTolerant"),
                    new RadioOptionPoint(NamingStyleKinds.AA_BB, "ALL_UPPER"),
                    new RadioOptionPoint(NamingStyleKinds.Aa_bb, "First_upper")
                });
            AddBoolOption(enabledProperty, "Enable inspection", null);
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
            AddHeader("Text based assets");
            AddBoolOption((UnitySettings s) => s.IsAssetIndexingEnabled,
                "Parse text based asset files for script and event handler usages");

            if (OptionsPageContext.IsRider)
            {
                AddBoolOption((UnitySettings s) => s.EnableInspectorPropertiesEditor,
                    "Show Inspector values in the editor");
            }

            AddBoolOption((UnitySettings s) => s.IsPrefabCacheEnabled,
                "Cache prefab data to improve find usage performance");
            AddBoolOption((UnitySettings s) => s.EnableAssetIndexingPerformanceHeuristic,
                "Automatically disable asset indexing for large solutions");

            if (OptionsPageContext.IsRider)
            {
                AddBoolOption((UnitySettings s) => s.UseUnityYamlMerge, "Prefer UnityYamlMerge for merging YAML files");
                using (Indent())
                {
                    var option = AddControl((UnitySettings s) => s.MergeParameters,
                        p => p.GetBeTextBox(Lifetime).WithDescription("Merge parameters", Lifetime));
                    BindToEnabledProperty(option, s => s.UseUnityYamlMerge);
                }
            }
        }

        private void AddShadersSection()
        {
            // TODO: For ReSharper, this is unavailable if the user hasn't installed ReSharper C++
            AddHeader("Shaders");
            AddBoolOption((UnitySettings s) => s.SuppressShaderErrorHighlighting,
                "Suppress resolve errors of unqualified names");
        }

        private void AddDebuggingSection()
        {
            if (!OptionsPageContext.IsRider) return;

            AddHeader("Debugging");
            AddBoolOption((UnitySettings s) => s.EnableDebuggerExtensions,
                "Extend value rendering");
            AddBetterCommentText("When enabled, Rider will show extra values in debugger object views,\n" +
                                 "such as active scene and GameObject component data and children.\n" +
                                 "Rendering of summary values is also improved, such as showing Vector3\n" +
                                 "float values with full precision.");

            AddBoolOption((UnitySettings s) => s.IgnoreBreakOnUnhandledExceptionsForIl2Cpp,
                "Ignore 'Break on unhandled exceptions' setting for IL2CPP players");
            AddBetterCommentText("Unity's Mono 4.x runtime ignores the 'Break on unhandled exceptions' setting.\n" +
                                 "This option applies the same behaviour to IL2CPP players.");
        }

        private void AddInternalSection(RunsProducts.ProductConfigurations productConfigurations)
        {
            if (!productConfigurations.IsInternalMode()) return;

            AddHeader("Internal");

            AddBoolOption((UnitySettings s) => s.SuppressShaderErrorHighlightingInRenderPipelinePackages,
                "Suppress resolve errors in render-pipeline packages");

            AddBoolOption((UnitySettings s) => s.EnableCgErrorHighlighting,
                "[Deprecated] Parse GLSL files for syntax errors (requires internal mode, and re-opening solution)");
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