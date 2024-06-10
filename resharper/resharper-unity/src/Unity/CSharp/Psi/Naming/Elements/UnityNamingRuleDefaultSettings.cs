using System;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Implementation;
using JetBrains.ReSharper.Psi.CSharp.Naming2;
using JetBrains.ReSharper.Psi.Naming.Elements;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.Util;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.Naming.Elements
{
    [ShellComponent(Instantiation.DemandAnyThreadSafe)]
    public class UnityNamingRuleDefaultSettings(ILogger logger, ISettingsSchema settingsSchema)
        : HaveDefaultSettings(settingsSchema, logger)
    {
        private static readonly Guid ourSerializedFieldRuleGuid = new("5F0FDB63-C892-4D2C-9324-15C80B22A7EF");

        public override string Name => "Unity default naming rules";

        public override void InitDefaultSettings(ISettingsStorageMountPoint mountPoint)
        {
            SetIndexedValue(mountPoint, (CSharpNamingSettings key) => key.UserRules, ourSerializedFieldRuleGuid,
                CreateDefaultUnitySerializedFieldRule());
        }

        public static ClrUserDefinedNamingRule CreateDefaultUnitySerializedFieldRule()
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
    }
}
