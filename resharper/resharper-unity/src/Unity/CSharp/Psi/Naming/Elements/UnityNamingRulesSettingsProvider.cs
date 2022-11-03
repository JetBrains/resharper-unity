#nullable enable
using System;
using System.Linq.Expressions;
using JetBrains.Application.DataContext;
using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Psi.CSharp.Naming2;
using JetBrains.ReSharper.Psi.Naming.Elements;
using JetBrains.ReSharper.Psi.Naming.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Naming.Elements
{
    [SolutionComponent]
    public class UnityNamingRulesSettingsProvider : IUnitySolutionSettingsProvider
    {
        private static readonly Expression<Func<CSharpNamingSettings, IIndexedEntry<Guid, ClrUserDefinedNamingRule>>>
            ourUserRulesAccessor = s => s.UserRules;

        private static readonly Guid SerializedFieldRuleGuid = new("5F0FDB63-C892-4D2C-9324-15C80B22A7EF");

        private readonly IContextBoundSettingsStore myContextBoundSettingStore;
        private readonly SettingsScalarEntry myEnableSerializedFieldNamingRule;


        public UnityNamingRulesSettingsProvider(Lifetime lifetime, ISettingsStore settingsStore, ISolution solution, DataContexts dataContexts)
        {
            var dataContext = solution.ToDataContext()(lifetime, dataContexts);
            myContextBoundSettingStore = settingsStore.BindToContext(dataContext);
            myEnableSerializedFieldNamingRule = settingsStore.Schema.GetScalarEntry( (UnitySettings s) => s.EnableSerializedFieldNamingRule);
        }

        public void InitialiseSolutionSettings(ISettingsStorageMountPoint mountPoint)
        {
            var entry = myContextBoundSettingStore.Schema.GetIndexedEntry(ourUserRulesAccessor);

            var value = myContextBoundSettingStore.GetValue(myEnableSerializedFieldNamingRule, null);
            if(value == null || ((bool)value) == false)
                RemoveUnitySerializedFieldRule(myContextBoundSettingStore, entry);
            else
            {
                var rule = GetUnitySerializedFieldRule(myContextBoundSettingStore, entry);
                SetUnitySerializedFieldRule(myContextBoundSettingStore, entry, rule);
            }
        }

        private static ClrUserDefinedNamingRule GetUnitySerializedFieldRule()
        {
            var lowerCaseNamingPolicy = new NamingPolicy(new NamingRule {NamingStyleKind = NamingStyleKinds.aaBb});
            return new ClrUserDefinedNamingRule(
                new ClrNamedElementDescriptor(
                    AccessRightKinds.Any,
                    StaticnessKinds.Instance,
                    new ElementKindSet(UnityNamedElement.SERIALISED_FIELD),
                    "Unity serialized field"),
                lowerCaseNamingPolicy
            );
        }

        public static ClrUserDefinedNamingRule GetUnitySerializedFieldRule(IContextBoundSettingsStore settingsStore,
            SettingsIndexedEntry entry)
        {
            if (settingsStore.GetIndexedValue(entry, SerializedFieldRuleGuid, null) is ClrUserDefinedNamingRule userRule)
                return userRule;
            
            userRule = GetUnitySerializedFieldRule();
            SetUnitySerializedFieldRule(settingsStore, entry, userRule);

            return userRule;
        }

        public static void SetUnitySerializedFieldRule(IContextBoundSettingsStore settingsStore,
            SettingsIndexedEntry entry, ClrUserDefinedNamingRule userRule)
        {
            settingsStore.SetIndexedValue(entry, SerializedFieldRuleGuid, null, userRule);
        }

        public static void RemoveUnitySerializedFieldRule(IContextBoundSettingsStore settingsStore, SettingsIndexedEntry entry)
        {
            settingsStore.RemoveIndexedValue(entry, SerializedFieldRuleGuid, null);
        }
    }
}