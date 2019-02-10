using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.MoveQuickFixes;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class MulOrderQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\MulOrder\Availability";

        [Test] public void Available01() { DoNamedTest(); }
        [Test] public void Available02() { DoNamedTest(); }
        [Test] public void UnavailableBrackets() { DoNamedTest(); }
        [Test] public void UnavailableGoodOrder() { DoNamedTest(); }

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile psiSourceFile,
            IContextBoundSettingsStore boundSettingsStore)
        {
            return highlighting is InefficientMultiplyOrderWarning;
        }
    }

    
    [TestUnity]
    public class MulOrderQuickFixTests : CSharpQuickFixTestBase<MulOrderQuickFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\MulOrder";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
    }
}