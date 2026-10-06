using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class InvalidSignatureFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\InvalidSignature\Availability";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid signature")] public void AddAllParameters() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid signature")] public void AddMissingParameter() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid signature")] public void RemoveAllParameters() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid signature")] public void RemoveAllParametersThreeParameters() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid signature")] public void RenameParameters() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid signature")] public void ReorderParameters() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid signature")] public void InitializeOnLoadMethod() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid signature")] public void RuntimeInitializeOnLoadMethod() { DoNamedTest(); }

        private static readonly string[] ourAddMissingParametersSource = {"000", "010", "011", "100", "101", "110" };
        private static readonly string[] ourReorderParametersSource = {"132", "213", "231", "312", "321"};

        [TestCaseSource(nameof(ourAddMissingParametersSource))]
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid signature")] public void AddMissingParameterThreeParameters(string id) { DoOneTest($"{TestMethodName}_{id}"); }

        [TestCaseSource(nameof(ourReorderParametersSource))]
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid signature")] public void ReorderParametersThreeParameters(string id) { DoOneTest($"{TestMethodName}_{id}"); }
    }

    [TestUnity]
    public class InvalidSignatureFixTests : CSharpQuickFixTestBase<IncorrectMethodSignatureQuickFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\InvalidSignature";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid signature")] public void AddAllParameters() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid signature")] public void AddMissingParameter() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid signature")] public void RemoveAllParameters() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid signature")] public void RemoveAllParametersThreeParameters() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid signature")] public void RenameParameters() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid signature")] public void ReorderParameters() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid signature")] public void InitializeOnLoadMethod() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid signature")] public void RuntimeInitializeOnLoadMethod() { DoNamedTest(); }

        private static readonly string[] ourAddMissingParametersSource = {"000", "010", "011", "100", "101", "110" };
        private static readonly string[] ourReorderParametersSource = {"132", "213", "231", "312", "321"};

        [TestCaseSource(nameof(ourAddMissingParametersSource))]
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid signature")] public void AddMissingParameterThreeParameters(string id) { DoOneTest($"{TestMethodName}_{id}"); }

        [TestCaseSource(nameof(ourReorderParametersSource))]
        [Test, UnityPluginBackendChecklist("Quick fixes", "Event functions", "Invalid signature")] public void ReorderParametersThreeParameters(string id) { DoOneTest($"{TestMethodName}_{id}"); }
    }
}