using System;
using System.Linq.Expressions;
using JetBrains.Application.Environment;
using JetBrains.Application.Environment.Helpers;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.Options;
using JetBrains.Application.UI.Options.OptionsDialog.SimpleOptions;
using JetBrains.Application.UI.Options.OptionsDialog.SimpleOptions.ViewModel;
using JetBrains.DataFlow;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.OptionPages.CodeEditing;
using JetBrains.ReSharper.Plugins.Unity.Application.UI.Options;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Naming.Elements;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi.CSharp.Naming2;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [OptionsPage(PID, Name, typeof(LogoThemedIcons.UnityLogo), Sequence = 0.01,
        ParentId = CodeEditingPage.PID)]
    public class UnityOptionsPage : OptionsPageBase
    {
        // Keep these in sync with the values in the front end!
        public const string PID = "UnityPluginSettings";
        public const string Name = "Unity Engine";

        private static readonly Expression<Func<CSharpNamingSettings, IIndexedEntry<Guid, ClrUserDefinedNamingRule>>>
            ourUserRulesAccessor = s => s.UserRules;

        private static readonly Expression<Func<UnitySettings, bool>> ourEnablePerformanceHighlightingAccessor =
            s => s.EnablePerformanceCriticalCodeHighlighting;

        public UnityOptionsPage(Lifetime lifetime, OptionsSettingsSmartContext settingsStore,
                                RunsProducts.ProductConfigurations productConfigurations)
            : base(lifetime, settingsStore)
        {
            Header("General");
            CheckBox((UnitySettings s) => s.InstallUnity3DRiderPlugin,
                "Automatically install and update Rider's Unity editor plugin (recommended)");
            CheckBox((UnitySettings s) => s.AllowAutomaticRefreshInUnity, "Automatically refresh assets in Unity");

            Header("C#");
            CheckBox(ourEnablePerformanceHighlightingAccessor,
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

            AddComboOption((UnitySettings s) => s.GutterIconMode, "Show gutter icons for implicit script usages:",
                new RadioOptionPoint(GutterIconMode.Always, "Always"),
                new RadioOptionPoint(GutterIconMode.CodeInsightDisabled, "When Code Vision is disabled"),
                new RadioOptionPoint(GutterIconMode.None, "Never")
            );

            AddNamingSection(lifetime, settingsStore);

            Header("Text based assets");
            CheckBox((UnitySettings s) => s.IsAssetIndexingEnabled,
                "Parse text based asset files for script and event handler usages");
            CheckBox((UnitySettings s) => s.EnableInspectorPropertiesEditor,
                "Show Inspector values in the editor");
            CheckBox((UnitySettings s) => s.EnableInspectorPropertiesEditor,
                "Store prefab information in caches (improves find usages performance)");
            CheckBox((UnitySettings s) => s.EnableAssetIndexingPerformanceHeuristic,
                "Automatically disable asset indexing for large solutions");
            
            Header("ShaderLab");
            CheckBox((UnitySettings s) => s.EnableShaderLabHippieCompletion,
                "Enable simple word-based completion in ShaderLab files");

            if (productConfigurations.IsInternalMode())
            {
                Header("Internal");

                CheckBox((UnitySettings s) => s.EnableCgErrorHighlighting,
                    "Parse Cg files for syntax errors (requires internal mode, and re-opening solution)");
            }

            FinishPage();
        }

        private void AddNamingSection(Lifetime lifetime, IContextBoundSettingsStore settingsStore)
        {
            BeginSection();

            // Rider doesn't have a UI for editing user defined rules. See RIDER-8339
            Header("Serialized field naming rules");

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

            var enabledProperty = new Property<bool>(lifetime, "BoolOptionViewModel_SerializedFieldEnableInspection");
            enabledProperty.SetValue(userRule.Policy.EnableInspection);
            enabledProperty.Change.Advise_NoAcknowledgement(lifetime, args =>
            {
                var existingRule = GetUnitySerializedFieldRule(settingsStore, entry);
                var newRule = new ClrUserDefinedNamingRule(existingRule.Descriptor,
                    new NamingPolicy(existingRule.Policy.ExtraRules.ToIReadOnlyList(), existingRule.Policy.NamingRule, args.New));
                SetUnitySerializedFieldRule(settingsStore, entry, newRule);
            });

            WithIndent(AddStringOption(prefixProperty, "Prefix:"));
            WithIndent(AddStringOption(suffixProperty, "Suffix:"));
            WithIndent(AddComboOption(kindProperty, "Style:",
                new RadioOptionPoint(NamingStyleKinds.AaBb, "UpperCamelCase"),
                new RadioOptionPoint(NamingStyleKinds.AaBb_AaBb, "UpperCamelCase_UnderscoreTolerant"),
                new RadioOptionPoint(NamingStyleKinds.AaBb_aaBb, "UpperCamelCase_underscoreTolerant"),
                new RadioOptionPoint(NamingStyleKinds.aaBb, "lowerCamelCase"),
                new RadioOptionPoint(NamingStyleKinds.aaBb_AaBb, "lowerCamelCase_UnderscoreTolerant"),
                new RadioOptionPoint(NamingStyleKinds.aaBb_aaBb, "lowerCamelCase_underscoreTolerant"),
                new RadioOptionPoint(NamingStyleKinds.AA_BB, "ALL_UPPER"),
                new RadioOptionPoint(NamingStyleKinds.Aa_bb, "First_upper")));
            WithIndent(AddBoolOption(enabledProperty, "Enable inspection"));

            EndSection();
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

        private StringOptionViewModel AddStringOption(Property<string> property, string text, string toolTipText = null,
                                     bool acceptsReturn = false)
        {
            return Lifetime.Using(tempLifetime =>
            {
                // StringOptionViewModel doesn't allow us to pass a Property, but creates one based on the given scalar
                // entry. We're dealing with a custom object as a custom entry, so this doesn't work for us. Let's hack!
                // Create a StringOptionViewModel, with a binding to a scalar, let it create a property, then overwrite
                // it. The temp lifetime will then clean up the binding.
                // RIDER-8339 is sooo getting fixed in 2019.1
                var stringOptionViewModel = new StringOptionViewModel(tempLifetime,
                    OptionsSettingsSmartContext,
                    OptionsSettingsSmartContext.Schema.GetScalarEntry((CSharpNamingSettings s) => s.ExceptionName),
                    text, toolTipText ?? string.Empty, acceptsReturn) {StringProperty = property};

                OptionEntities.Add(stringOptionViewModel);
                return stringOptionViewModel;
            });
        }
    }
}
