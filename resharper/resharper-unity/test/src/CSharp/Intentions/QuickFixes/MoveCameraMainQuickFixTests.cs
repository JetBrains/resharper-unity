using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.MoveQuickFixes;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class MoveCameraMainQuickFixTests : CSharpQuickFixAfterSwaTestBase<MoveCameraMainQuickFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\MoveCameraMain";

        [Test] public void MoveToStart() { DoNamedTest(); }
        [Test] public void MoveToAwake() { DoNamedTest(); }
        [Test] public void MoveOutsideTheLoop() { DoNamedTest(); }
        [Test] public void CorrectNameGeneration() {DoNamedTest(); }
    }
}