using JetBrains.ReSharper.Plugins.Tests.TestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.MoveQuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class MoveCameraMainQuickFixTests : CSharpQuickFixAfterSwaTestBase<MoveCameraMainQuickFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\MoveCameraMain";

        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Move Camera.main out of update")] public void MoveToStart() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Move Camera.main out of update")] public void MoveToAwake() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Move Camera.main out of update")] public void MoveOutsideTheLoop() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Quick fixes", "Performance", "Move Camera.main out of update")] public void CorrectNameGeneration() {DoNamedTest(); }
    }
}