using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.Burst
{   
    [TestUnity]
    public class BurstStageTest : UnityGlobalHighlightingsStageTestBase
    {
        protected override string RelativeTestDataRoot => @"CSharp\Daemon\Stages\BurstCodeAnalysis\";
        [Test] public void ComplexTest() { DoNamedTest(); }
        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile file, IContextBoundSettingsStore settingsStore)
        {
            return highlighting is CSharpUnityHighlightingBase;
        }
    }    
}    