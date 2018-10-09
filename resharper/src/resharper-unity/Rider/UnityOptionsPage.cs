using System;
using System.Linq.Expressions;
using JetBrains.Application.Environment;
using JetBrains.Application.Environment.Helpers;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.Options;
using JetBrains.Application.UI.Options.OptionsDialog.SimpleOptions.ViewModel;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Feature.Services.OptionPages.CodeEditing;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Naming.Elements;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi.CSharp.Naming2;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [OptionsPage(PID, "Unity Engine", typeof(LogoThemedIcons.UnityLogo), Sequence = 0.01,
        ParentId = CodeEditingPage.PID)]
    public class UnityOptionsPage : OptionsPageBase
    {
        public const string PID = "UnityPluginSettings";

        private static readonly Expression<Func<CSharpNamingSettings, IIndexedEntry<Guid, ClrUserDefinedNamingRule>>>
            ourUserRulesAccessor = s => s.UserRules;

        public UnityOptionsPage(Lifetime lifetime, OptionsSettingsSmartContext settingsStore,
                                RunsProducts.ProductConfigurations productConfigurations)
            : base(lifetime, settingsStore)
        {
            Header("General");

            CheckBox((UnitySettings s) => s.InstallUnity3DRiderPlugin, "Install or update Rider plugin automatically");
            CheckBox((UnitySettings s) => s.AllowAutomaticRefreshInUnity, "Automatically refresh Assets in Unity");

            AddNamingSection(lifetime, settingsStore);

            Header("ShaderLab");

            CheckBox((UnitySettings s) => s.EnableShaderLabHippieCompletion,
                "Enable simple word-based completion in ShaderLab files");

            if (productConfigurations.IsInternalMode())
            {
                AddEmptyLine();
                CheckBox((UnitySettings s) => s.EnableCgErrorHighlighting,
                    "Parse Cg files for syntax errors. Only works in internal mode.");
                AddText("Requires solution reopen.");
            }
            
            Header("C# code analysis");
            CheckBox((UnitySettings s) => s.EnablePerformanceCriticalCodeHighlighting,
                "Enable highlighting of costly methods and indirect calls for these methods in performance critical code sections");
            
            FinishPage();
        }

        private void AddNamingSection(Lifetime lifetime, IContextBoundSettingsStore settingsStore)
        {
            // Rider doesn't have a UI for editing user defined rules. See RIDER-8339
#if RIDER
            Header("Naming");

            var entry = settingsStore.Schema.GetIndexedEntry(ourUserRulesAccessor);
            var userRule = GetUnitySerializedFieldRule(settingsStore, entry);
            Assertion.AssertNotNull(userRule, "userRule != null");

            var property = new Property<object>(lifetime, "ComboOptionViewModel_SerializedFieldNamingStyle");
            property.SetValue(userRule.Policy.NamingRule.NamingStyleKind);
            property.Change.Advise_NoAcknowledgement(lifetime, args =>
            {
                var rule = GetUnitySerializedFieldRule(settingsStore, entry);
                rule.Policy.NamingRule.NamingStyleKind = (NamingStyleKinds) args.New;
                SetUnitySerializedFieldRule(settingsStore, entry, rule);
            });

            AddComboOption(property, "Naming style for serialized fields:",
                new RadioOptionPoint(NamingStyleKinds.AaBb, "UpperCamelCase"),
                new RadioOptionPoint(NamingStyleKinds.AaBb_AaBb, "UpperCamelCase_UnderscoreTolerant"),
                new RadioOptionPoint(NamingStyleKinds.AaBb_aaBb, "UpperCamelCase_underscoreTolerant"),
                new RadioOptionPoint(NamingStyleKinds.aaBb, "lowerCamelCase"),
                new RadioOptionPoint(NamingStyleKinds.aaBb_AaBb, "lowerCamelCase_UnderscoreTolerant"),
                new RadioOptionPoint(NamingStyleKinds.aaBb_aaBb, "lowerCamelCase_underscoreTolerant"),
                new RadioOptionPoint(NamingStyleKinds.AA_BB, "ALL_UPPER"),
                new RadioOptionPoint(NamingStyleKinds.Aa_bb, "First_upper"));
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
#endif
        }
    }
}
