using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class InvalidReturnTypeFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\InvalidReturnType\Availability";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
        [Test] public void Test03() { DoNamedTest(); }
        [Test] public void InitializeOnLoadMethod() { DoNamedTest(); }
        [Test] public void RuntimeInitializeOnLoadMethod() { DoNamedTest(); }
    }

    [TestUnity]
    public class InvalidReturnTypeFixTests : CSharpQuickFixTestBase<IncorrectMethodSignatureQuickFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\InvalidReturnType";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
        [Test] public void Test03() { DoNamedTest(); }
        [Test] public void InitializeOnLoadMethod() { DoNamedTest(); }
        [Test] public void RuntimeInitializeOnLoadMethod() { DoNamedTest(); }
    }
}