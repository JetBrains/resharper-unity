using System;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.ReSharper.Psi.CSharp.Naming2;
using JetBrains.ReSharper.Psi.Naming.Elements;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Naming.Elements
{
    // Defining an element kind isn't enough, we also need to set up a rule that uses it. This is a different process
    // for CLR and non-CLR languages. The rules come from a per-language instance of INamingPolicyProvider, which gets
    // its values from settings. For non-CLR languages, this is simply a map from element kind name to NamingPolicy,
    // which is a list of NamingRules and a flag for inspections. If the rule isn't set up, the default NamingRule comes
    // from IElementKind.GetDefaultRule.
    // CLR languages have a more flexible (but also more confusing) system. There are two sets of rules, predefined and
    // user. The predefined rules are a map of enum to NamingPolicy, with hardcoded defaults. The NamingPolicy can be
    // modified, but the rule cannot be deleted. The user rules are more flexible, with a set of element kinds, flags
    // for static and instance modifiers, plus the NamingPolicy. These have to be set in defaults, and can be both
    // modified and deleted.
    // I'm not really sure why there is this split. AFAICT, the predefined rules could all be handled with user rules,
    // if a "do not delete" flag was added. One thing these rules provide is a set of fallbacks, so that there is no
    // need to provide default rules for all possible element kinds. For example, the XAML namespace naming rule can
    // fallback to the TypesAndNamespaces predefined type if XamlNamedElements.NAMESPACE_ALIAS doesn't have a rule.
    // Whatever, this class adds a default user rule for our Unity element kinds
    [ShellComponent]
    public class UnityNamingRuleDefaultSettings : HaveDefaultSettings
    {
        public static readonly Guid SerializedFieldRuleGuid = new Guid("5F0FDB63-C892-4D2C-9324-15C80B22A7EF");

        public UnityNamingRuleDefaultSettings(ILogger logger, ISettingsSchema settingsSchema)
            : base(logger, settingsSchema)
        {
        }

        public override void InitDefaultSettings(ISettingsStorageMountPoint mountPoint)
        {
            SetIndexedValue(mountPoint, (CSharpNamingSettings key) => key.UserRules, SerializedFieldRuleGuid,
                GetUnitySerializedFieldRule());
        }

        public static ClrUserDefinedNamingRule GetUnitySerializedFieldRule()
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

        public override string Name => "Unity default naming rules";
    }
}
