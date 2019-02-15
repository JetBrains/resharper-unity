using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
#if RIDER
using JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights;
#endif
using JetBrains.ReSharper.Psi;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class UnityEventFunctionAnalyzerTests : CSharpHighlightingTestWithProductDependentGoldBase<IUnityHighlighting>
    {
        protected override string RelativeTestDataRoot => @"CSharp\Daemon\Stages\Analysis";

        [Test] public void TestUnityEventFunctionAnalyzer() { DoNamedTest2(); }


        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile,
            IContextBoundSettingsStore settingsStore)
        {
            #if RIDER
            return highlighting is UnityImplicitlyUsedIdentifierHighlighting || highlighting is UnityCodeInsightsHighlighting;
            #else
            return highlighting is UnityImplicitlyUsedIdentifierHighlighting || highlighting is UnityGutterMarkInfo;

            #endif
        }
    }
}