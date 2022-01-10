using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.UnityHighlightings
{
    [TestUnity]
    public class UnityHighlightingStageTest : UnityGlobalHighlightingsStageTestBase<IUnityHighlighting>
    {
        protected override string RelativeTestDataRoot => @"CSharp\Daemon\Stages\UnityHighlightingsStage\";

        [Test] public void CommonTest01() { DoNamedTest(); }
        [Test] public void CommonTest02() { DoNamedTest(); }
    }
}