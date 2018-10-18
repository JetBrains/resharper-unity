using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class InvalidSignatureFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\InvalidSignature\Availability";

        [Test] public void AddAllParameters() { DoNamedTest(); }
        [Test] public void AddMissingParameter() { DoNamedTest(); }
        [Test] public void RemoveAllParameters() { DoNamedTest(); }
        [Test] public void RemoveAllParametersThreeParameters() { DoNamedTest(); }
        [Test] public void RenameParameters() { DoNamedTest(); }
        [Test] public void ReorderParameters() { DoNamedTest(); }
        [Test] public void InitializeOnLoadMethod() { DoNamedTest(); }
        [Test] public void RuntimeInitializeOnLoadMethod() { DoNamedTest(); }

        private readonly string[] myAddMissingParametersSource = {"000", "010", "011", "100", "101", "110" };
        private readonly string[] myReorderParametersSource = {"132", "213", "231", "312", "321"};

        [TestCaseSource(nameof(myAddMissingParametersSource))]
        [Test] public void AddMissingParameterThreeParameters(string id) { DoOneTest($"{TestMethodName}_{id}"); }

        [TestCaseSource(nameof(myReorderParametersSource))]
        [Test] public void ReorderParametersThreeParameters(string id) { DoOneTest($"{TestMethodName}_{id}"); }
    }

    [TestUnity]
    public class InvalidSignatureFixTests : CSharpQuickFixTestBase<IncorrectMethodSignatureQuickFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\InvalidSignature";

        [Test] public void AddAllParameters() { DoNamedTest(); }
        [Test] public void AddMissingParameter() { DoNamedTest(); }
        [Test] public void RemoveAllParameters() { DoNamedTest(); }
        [Test] public void RemoveAllParametersThreeParameters() { DoNamedTest(); }
        [Test] public void RenameParameters() { DoNamedTest(); }
        [Test] public void ReorderParameters() { DoNamedTest(); }
        [Test] public void InitializeOnLoadMethod() { DoNamedTest(); }
        [Test] public void RuntimeInitializeOnLoadMethod() { DoNamedTest(); }

        private readonly string[] myAddMissingParametersSource = {"000", "010", "011", "100", "101", "110" };
        private readonly string[] myReorderParametersSource = {"132", "213", "231", "312", "321"};

        [TestCaseSource(nameof(myAddMissingParametersSource))]
        [Test] public void AddMissingParameterThreeParameters(string id) { DoOneTest($"{TestMethodName}_{id}"); }

        [TestCaseSource(nameof(myReorderParametersSource))]
        [Test] public void ReorderParametersThreeParameters(string id) { DoOneTest($"{TestMethodName}_{id}"); }
    }
}