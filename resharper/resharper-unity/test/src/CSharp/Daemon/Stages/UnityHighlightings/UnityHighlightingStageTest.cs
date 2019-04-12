using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.UnityHighlightings
{
    [TestUnity]
    public class UnityHighlightingStageTest : UnityGlobalHighlightingsStageTestBase
    {
        protected override string RelativeTestDataRoot => @"CSharp\Daemon\Stages\UnityHighlightingsStage\";

        [Test] public void CommonTest01() { DoNamedTest(); }
        [Test] public void CommonTest02() { DoNamedTest(); }

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile file, IContextBoundSettingsStore settingsStore)
        {
            return highlighting is IUnityHighlighting;
        }
    }
}