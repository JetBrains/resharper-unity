using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Tests.TestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.BurstCodeAnalysis.AddDiscardAttribute;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    // [TestUnity]
    // public class BurstDiscardAvailabilityTests : QuickFixAfterSwaAvailabilityTestBase
    // {
    //     protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\BurstDiscard\Availability";
    //
    //     [Test] public void Everything() { DoNamedTest(); }
    //
    //     protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile psiSourceFile,
    //         IContextBoundSettingsStore boundSettingsStore)
    //     {
    //         return highlighting is IBurstHighlighting;
    //     }
    // }

    // [TestUnity]
    // public class BurstDiscardQuickFixTests : CSharpQuickFixAfterSwaTestBase<AddDiscardAttributeQuickFix>
    // {
    //     protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\BurstDiscard";
    //
    //     [Test] public void TransitiveActions() { DoNamedTest(); }
    //     [Test] public void RootAction() { DoNamedTest(); }
    // }
}