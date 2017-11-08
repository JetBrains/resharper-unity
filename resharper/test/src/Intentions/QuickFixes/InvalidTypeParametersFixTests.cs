using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Intentions.QuickFixes
{
    [TestUnity]
    public class InvalidTypeParametersFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"Intentions\QuickFixes\InvalidTypeParameters\Availability";

        [Test] public void MonoBehaviourMethod() { DoNamedTest(); }
        [Test] public void InitializeOnLoadMethod() { DoNamedTest(); }
        [Test] public void RuntimeInitializeOnLoadMethod() { DoNamedTest(); }
    }

    [TestUnity]
    public class InvalidTypeParametersFixTests : CSharpQuickFixTestBase<InvalidTypeParametersFix>
    {
        protected override string RelativeTestDataPath=> @"Intentions\QuickFixes\InvalidTypeParameters";

        [Test] public void MonoBehaviourMethod() { DoNamedTest(); }
        [Test] public void InitializeOnLoadMethod() { DoNamedTest(); }
        [Test] public void RuntimeInitializeOnLoadMethod() { DoNamedTest(); }
    }
}