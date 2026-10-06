using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class RedundantSerializeFieldAttributeQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\RedundantSerializeFieldAttribute\Availability";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Remove redundant SerializeField attribute")] public void Test01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Remove redundant SerializeField attribute")] public void Test02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Remove redundant SerializeField attribute")] public void Test03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Remove redundant SerializeField attribute")] public void Test04() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Remove redundant SerializeField attribute")] public void Test05() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Remove redundant SerializeField attribute")] public void Test06() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Remove redundant SerializeField attribute")] public void Test07() { DoNamedTest(); }

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile psiSourceFile,
            IContextBoundSettingsStore boundSettingsStore)
        {
            return highlighting is RedundantSerializeFieldAttributeWarning;
        }
    }

    [TestUnity]
    public class RedundantSerializeFieldAttributeQuickFixTests : CSharpQuickFixTestBase<RemoveRedundantAttributeQuickFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\RedundantSerializeFieldAttribute";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Remove redundant SerializeField attribute")] public void Test01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Remove redundant SerializeField attribute")] public void Test02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Remove redundant SerializeField attribute")] public void Test03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Remove redundant SerializeField attribute")] public void Test04() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Remove redundant SerializeField attribute")] public void Test05() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Remove redundant SerializeField attribute")] public void Test06() { DoNamedTest(); }
    }
}