using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class RedundantHideInInspectorAttributeQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\RedundantHideInInspectorAttribute\Availability";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Remove redundant HideInInspector attribute")] public void Test01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Remove redundant HideInInspector attribute")] public void Test02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Remove redundant HideInInspector attribute")] public void Test03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Remove redundant HideInInspector attribute")] public void Test04() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Remove redundant HideInInspector attribute")] public void Test05() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Remove redundant HideInInspector attribute")] public void Test06() { DoNamedTest(); }
    }

    [TestUnity]
    public class RedundantHideInInspectorAttributeQuickFixTests : CSharpQuickFixTestBase<RemoveRedundantAttributeQuickFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\RedundantHideInInspectorAttribute";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Remove redundant HideInInspector attribute")] public void Test01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Remove redundant HideInInspector attribute")] public void Test02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Remove redundant HideInInspector attribute")] public void Test03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Remove redundant HideInInspector attribute")] public void Test04() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Remove redundant HideInInspector attribute")] public void Test05() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Serialization", "Remove redundant HideInInspector attribute")] public void Test06() { DoNamedTest(); }
    }
}