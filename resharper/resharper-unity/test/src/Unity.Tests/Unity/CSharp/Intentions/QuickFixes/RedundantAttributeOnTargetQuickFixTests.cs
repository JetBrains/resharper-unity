using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class RedundantAttributeOnTargetQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\RedundantAttributeOnTarget\Availability";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Attributes", "Remove redundant attribute on target")] public void TestRedundantAssemblyAttribute() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Attributes", "Remove redundant attribute on target")] public void TestRedundantClassAttribute() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Attributes", "Remove redundant attribute on target")] public void TestRedundantFieldAttribute() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Attributes", "Remove redundant attribute on target")] public void TestRedundantDelegateAttribute() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Attributes", "Remove redundant attribute on target")] public void TestRedundantAttributesInScope() { DoNamedTest2(); }
    }

    [TestUnity]
    public class RedundantAttributeOnTargetQuickFixRemoveTests : CSharpQuickFixTestBase<RemoveRedundantAttributeQuickFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\RedundantAttributeOnTarget";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Attributes", "Remove redundant attribute on target")] public void TestRedundantAssemblyAttribute() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Attributes", "Remove redundant attribute on target")] public void TestRedundantClassAttribute() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Attributes", "Remove redundant attribute on target")] public void TestRedundantFieldAttribute() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Attributes", "Remove redundant attribute on target")] public void TestRedundantDelegateAttribute() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Attributes", "Remove redundant attribute on target")] public void TestRedundantAttributesInScope() { DoNamedTest2(); }
    }
}