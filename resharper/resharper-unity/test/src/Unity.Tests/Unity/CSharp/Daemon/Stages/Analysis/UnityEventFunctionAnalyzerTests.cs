using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;
#if RIDER
using JetBrains.ReSharper.Plugins.Unity.Rider.CSharp.Daemon.CodeInsights;
#endif

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
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
            return highlighting is UnityImplicitlyUsedIdentifierHighlighting || highlighting is UnityGutterMarkInfo || highlighting is UnityHotGutterMarkInfo;

            #endif
        }
    }
}