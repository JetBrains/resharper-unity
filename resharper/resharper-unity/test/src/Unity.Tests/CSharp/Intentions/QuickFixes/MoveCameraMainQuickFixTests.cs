using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.MoveQuickFixes;
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