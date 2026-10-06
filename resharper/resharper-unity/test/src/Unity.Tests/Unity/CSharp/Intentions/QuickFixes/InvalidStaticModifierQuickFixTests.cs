using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class InvalidStaticModifierQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\InvalidStaticModifier\Availability";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid static modifier")] public void Test01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid static modifier")] public void Test02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid static modifier")] public void InitializeOnLoadMethod() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid static modifier")] public void RuntimeInitializeOnLoadMethod() { DoNamedTest(); }
    }

    [TestUnity]
    public class InvalidStaticModifierQuickFixTests : CSharpQuickFixTestBase<IncorrectMethodSignatureQuickFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\InvalidStaticModifier";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid static modifier")] public void Test01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid static modifier")] public void Test02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid static modifier")] public void InitializeOnLoadMethod() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid static modifier")] public void RuntimeInitializeOnLoadMethod() { DoNamedTest(); }
    }
}