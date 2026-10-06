using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class InvalidTypeParametersFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\InvalidTypeParameters\Availability";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid type parameters")] public void MonoBehaviourMethod() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid type parameters")] public void InitializeOnLoadMethod() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid type parameters")] public void RuntimeInitializeOnLoadMethod() { DoNamedTest(); }
    }

    [TestUnity]
    public class InvalidTypeParametersFixTests : CSharpQuickFixTestBase<IncorrectMethodSignatureQuickFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\InvalidTypeParameters";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid type parameters")] public void MonoBehaviourMethod() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid type parameters")] public void InitializeOnLoadMethod() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid type parameters")] public void RuntimeInitializeOnLoadMethod() { DoNamedTest(); }
    }
}
