using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CommonCodeAnalysis.Highlightings;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class CommonProblemAnalyzersTests : UnityGlobalHighlightingsStageTestBase
    {
        protected override string RelativeTestDataRoot => @"CSharp\Daemon\Stages\CommonCodeAnalysis\";
        [Test] public void SharedStaticTests() { DoNamedTest(); }
        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile file, IContextBoundSettingsStore settingsStore)
        {
            return highlighting is ICommonCodeHighlighting;
        }
    }
}