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
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Naming.Elements;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi.CSharp.Naming2;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [OptionsPage(PID, Name, typeof(LogoIcons.Unity), Sequence = 0.01,
        ParentId = CodeEditingPage.PID)]
    public class UnityOptionsPage : BeSimpleOptionsPage
    {
        // Keep these in sync with the values in the front end!
        public const string PID = "UnityPluginSettings";
        public const string Name = "Unity Engine";

        private static readonly Expression<Func<CSharpNamingSettings, IIndexedEntry<Guid, ClrUserDefinedNamingRule>>>
            ourUserRulesAccessor = s => s.UserRules;

        private static readonly Expression<Func<UnitySettings, bool>> ourEnablePerformanceHighlightingAccessor =
            s => s.EnablePerformanceCriticalCodeHighlighting;

        private static readonly Expression<Func<UnitySettings, bool>> ourEnableBurstHighlightingAccessor =
            s => s.EnableBurstCodeHighlighting;

        public UnityOptionsPage(Lifetime lifetime, OptionsPageContext pageContext,
                                OptionsSettingsSmartContext settingsStore,
                                RunsProducts.ProductConfigurations productConfigurations)
            : base(lifetime, pageContext, settingsStore)
        {
            AddHeader("General");
            AddBoolOption((UnitySettings s) => s.InstallUnity3DRiderPlugin,
                "Automatically install and update Rider's Unity editor plugin (recommended)");
            AddBoolOption((UnitySettings s) => s.AllowAutomaticRefreshInUnity, "Automatically refresh assets in Unity");

            AddHeader("C#");
            AddBoolOption(ourEnableBurstHighlightingAccessor, "Enable analysis for Burst compiler issues");
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

            AddComboOption((UnitySettings s) => s.GutterIconMode,
                "Show gutter icons for implicit script usages:", string.Empty, string.Empty,
                new RadioOptionPoint(GutterIconMode.Always, "Always"),
                new RadioOptionPoint(GutterIconMode.CodeInsightDisabled, "When Code Vision is disabled"),
                new RadioOptionPoint(GutterIconMode.None, "Never")
            );

            AddNamingSection(lifetime, settingsStore);

            AddHeader("Text based assets");
            AddBoolOption((UnitySettings s) => s.IsAssetIndexingEnabled,
                "Parse text based asset files for script and event handler usages");
            AddBoolOption((UnitySettings s) => s.EnableInspectorPropertiesEditor,
                "Show Inspector values in the editor");
            AddBoolOption((UnitySettings s) => s.IsPrefabCacheEnabled,
                "Cache prefab data to improve find usage performance");
            AddBoolOption((UnitySettings s) => s.EnableAssetIndexingPerformanceHeuristic,
                "Automatically disable asset indexing for large solutions");
            AddBoolOption((UnitySettings s) => s.UseUnityYamlMerge, "Prefer UnityYamlMerge for merging YAML files");
            using(Indent())
            {
                var option = AddControl((UnitySettings s) => s.MergeParameters,
                    p => p.GetBeTextBox(lifetime).WithDescription("Merge parameters", lifetime));
                AddBinding(option, BindingStyle.IsEnabledProperty, (Expression<Func<UnitySettings, bool>>) (s => s.UseUnityYamlMerge),
                    enable => enable);
            }

            AddHeader("ShaderLab");
            AddBoolOption((UnitySettings s) => s.EnableShaderLabHippieCompletion,
                "Enable simple word-based completion in ShaderLab files");

            if (productConfigurations.IsInternalMode())
            {
                AddHeader("Internal");

                AddBoolOption((UnitySettings s) => s.EnableCgErrorHighlighting,
                    "Parse Cg files for syntax errors (requires internal mode, and re-opening solution)");
            }
        }

        private void AddNamingSection(Lifetime lifetime, IContextBoundSettingsStore settingsStore)
        {
            using (Indent())
            {
                // Rider doesn't have a UI for editing user defined rules. See RIDER-8339
                AddHeader("Serialized field naming rules");

                var entry = settingsStore.Schema.GetIndexedEntry(ourUserRulesAccessor);
                var userRule = GetUnitySerializedFieldRule(settingsStore, entry);
                Assertion.AssertNotNull(userRule, "userRule != null");

                var prefixProperty = new Property<string>(lifetime, "StringOptionViewModel_SerializedFieldPrefix");
                prefixProperty.SetValue(userRule.Policy.NamingRule.Prefix);
                prefixProperty.Change.Advise_NoAcknowledgement(lifetime, args =>
                {
                    var rule = GetUnitySerializedFieldRule(settingsStore, entry);
                    rule.Policy.NamingRule.Prefix = args.New ?? string.Empty;
                    SetUnitySerializedFieldRule(settingsStore, entry, rule);
                });

                var suffixProperty = new Property<string>(lifetime, "StringOptionViewModel_SerializedFieldSuffix");
                suffixProperty.SetValue(userRule.Policy.NamingRule.Suffix);
                suffixProperty.Change.Advise_NoAcknowledgement(lifetime, args =>
                {
                    var rule = GetUnitySerializedFieldRule(settingsStore, entry);
                    rule.Policy.NamingRule.Suffix = args.New ?? string.Empty;
                    SetUnitySerializedFieldRule(settingsStore, entry, rule);
                });

                var kindProperty = new Property<object>(lifetime, "ComboOptionViewModel_SerializedFieldNamingStyle");
                kindProperty.SetValue(userRule.Policy.NamingRule.NamingStyleKind);
                kindProperty.Change.Advise_NoAcknowledgement(lifetime, args =>
                {
                    var rule = GetUnitySerializedFieldRule(settingsStore, entry);
                    rule.Policy.NamingRule.NamingStyleKind = (NamingStyleKinds) args.New;
                    SetUnitySerializedFieldRule(settingsStore, entry, rule);
                });

                var enabledProperty =
                    new Property<bool>(lifetime, "BoolOptionViewModel_SerializedFieldEnableInspection");
                enabledProperty.SetValue(userRule.Policy.EnableInspection);
                enabledProperty.Change.Advise_NoAcknowledgement(lifetime, args =>
                {
                    var existingRule = GetUnitySerializedFieldRule(settingsStore, entry);
                    var newRule = new ClrUserDefinedNamingRule(existingRule.Descriptor,
                        new NamingPolicy(existingRule.Policy.ExtraRules.ToIReadOnlyList(),
                            existingRule.Policy.NamingRule, args.New));
                    SetUnitySerializedFieldRule(settingsStore, entry, newRule);
                });

                AddStringOption(lifetime, prefixProperty, "Prefix:");
                AddStringOption(lifetime, suffixProperty, "Suffix:");
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

        private void AddStringOption(Lifetime lifetime, IProperty<string> property, string text)
        {
            AddControlWithProperty(property, p => p.GetBeTextBox(lifetime).WithDescription(text, lifetime));
        }
    }
}
