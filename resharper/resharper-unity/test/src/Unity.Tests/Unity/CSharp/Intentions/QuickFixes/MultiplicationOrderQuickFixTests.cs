using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class MultiplicationOrderQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\MultiplicationOrder\Availability";

        [Test] public void Available01() { DoNamedTest(); }
        [Test] public void Available02() { DoNamedTest(); }
        [Test] public void Unavailable01() { DoNamedTest(); } // https://youtrack.jetbrains.com/issue/RIDER-33981
        [Test] public void UnavailableBrackets() { DoNamedTest(); }
        [Test] public void UnavailableGoodOrder() { DoNamedTest(); }
        [Test] public void UnavailableDiv() { DoNamedTest(); }

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile psiSourceFile,
            IContextBoundSettingsStore boundSettingsStore)
        {
            return highlighting is InefficientMultiplicationOrderWarning;
        }
    }


    [TestUnity]
    public class MultiplicationOrderQuickFixTests : CSharpQuickFixTestBase<MultiplicationOrderQuickFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\MultiplicationOrder";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
        [Test] public void Test03() { DoNamedTest(); }
        [Test] public void Test04() { DoNamedTest(); }
        [Test] public void Test05() { DoNamedTest(); }
        [Test] public void Test06() { DoNamedTest(); }
        [Test] public void Test07() { DoNamedTest(); }
        [Test] public void Test08() { DoNamedTest(); }
    }
}