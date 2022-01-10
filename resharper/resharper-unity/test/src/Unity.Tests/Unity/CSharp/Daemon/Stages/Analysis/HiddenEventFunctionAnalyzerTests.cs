using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class HiddenEventFunctionAnalyzerTests : CSharpHighlightingTestWithProductDependentGoldBase<IUnityAnalyzerHighlighting>
    {
        protected override string RelativeTestDataRoot => @"CSharp\Daemon\Stages\Analysis";

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile,
                                                      IContextBoundSettingsStore settingsStore)
        {
            return base.HighlightingPredicate(highlighting, sourceFile, settingsStore) ||
                   highlighting is InheritanceMarkOnGutter;
        }

        [Test] public void TestHiddenEventFunctions() { DoNamedTest2(); }
    }
}