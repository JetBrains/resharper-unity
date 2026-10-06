using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi.GenerateMemberBody;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class RedundantInitializeOnLoadAttributeQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\RedundantInitializeOnLoadAttribute\Availability";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Attributes", "Remove redundant InitializeOnLoad attribute")] public void Test01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Attributes", "Remove redundant InitializeOnLoad attribute")] public void Test02() { DoNamedTest(); }

        // Test06 so we can share files between availability and action tests
        [Test, UnityPluginBackendChecklist("Quick fixes", "Attributes", "Remove redundant InitializeOnLoad attribute")] public void Test06() { DoNamedTest(); }
    }

    [TestUnity]
    public class RedundantInitializeOnLoadAttributeQuickFixRemoveTests : CSharpQuickFixTestBase<RemoveRedundantAttributeQuickFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\RedundantInitializeOnLoadAttribute";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Attributes", "Remove redundant InitializeOnLoad attribute")] public void Test01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Attributes", "Remove redundant InitializeOnLoad attribute")] public void Test02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Attributes", "Remove redundant InitializeOnLoad attribute")] public void Test06() { DoNamedTest(); }
    }

    [TestUnity]
    [TestSetting(typeof(GenerateMemberBodySettings), nameof(GenerateMemberBodySettings.MethodImplementationKind), MethodImplementationKind.ThrowNotImplemented)]
    public class RedundantInitializeOnLoadAttributeQuickFixCreateTests : CSharpQuickFixTestBase<CreateFromUsageFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\RedundantInitializeOnLoadAttribute";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Attributes", "Remove redundant InitializeOnLoad attribute")] public void Test03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Attributes", "Remove redundant InitializeOnLoad attribute")] public void Test04() { DoNamedTest(); }

        [Test, TestSetting(typeof(GenerateMemberBodySettings), nameof(GenerateMemberBodySettings.MethodImplementationKind), MethodImplementationKind.ReturnDefaultValue)]
        [UnityPluginBackendChecklist("Quick fixes", "Attributes", "Remove redundant InitializeOnLoad attribute")]
        public void Test05() { DoNamedTest(); }
    }
}