using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class IncorrectMethodSignatureFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\IncorrectMethodSignature\Availability";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Incorrect method signature")] public void Test01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Incorrect method signature")] public void Test02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Incorrect method signature")] public void Test03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Incorrect method signature")] public void InitializeOnLoadMethod() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Incorrect method signature")] public void RuntimeInitializeOnLoadMethod() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Incorrect method signature")] public void MultipleRequiredSignatureCandidates01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Incorrect method signature")] public void MultipleRequiredSignatureCandidates02() { DoNamedTest(); }
    }

    [TestUnity]
    public class IncorrectMethodSignatureFixTests : CSharpQuickFixTestBase<IncorrectMethodSignatureQuickFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\IncorrectMethodSignature";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Incorrect method signature")] public void Test01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Incorrect method signature")] public void Test02() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Incorrect method signature")] public void Test03() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Incorrect method signature")] public void InitializeOnLoadMethod() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Incorrect method signature")] public void RuntimeInitializeOnLoadMethod() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Incorrect method signature")] public void MultipleRequiredSignatureCandidates01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Incorrect method signature")] public void MultipleRequiredSignatureCandidates02() { DoNamedTest(); }
    }
}