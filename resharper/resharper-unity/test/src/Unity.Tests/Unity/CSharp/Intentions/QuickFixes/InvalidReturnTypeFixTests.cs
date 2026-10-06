using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class InvalidReturnTypeFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\InvalidReturnType\Availability";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid return type")] public void Test01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid return type")] public void Test02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid return type")] public void Test03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid return type")] public void InitializeOnLoadMethod() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid return type")] public void RuntimeInitializeOnLoadMethod() { DoNamedTest(); }
    }

    [TestUnity]
    public class InvalidReturnTypeFixTests : CSharpQuickFixTestBase<IncorrectMethodSignatureQuickFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\InvalidReturnType";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid return type")] public void Test01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid return type")] public void Test02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid return type")] public void Test03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid return type")] public void InitializeOnLoadMethod() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid return type")] public void RuntimeInitializeOnLoadMethod() { DoNamedTest(); }
    }
}