using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.GutterMarks
{
    [ElementProblemAnalyzer(typeof(IFieldDeclaration), HighlightingTypes = new[]
    {
        typeof(UnityGutterMarkInfo),
#if RIDER
        typeof(JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights.UnityCodeInsightsHighlighting)
#endif
    })]
    public class UnityFieldDetector : UnityElementProblemAnalyzer<IFieldDeclaration>
    {
        private readonly UnityImplicitUsageHighlightingContributor myImplicitUsageHighlightingContributor;

        public UnityFieldDetector(UnityApi unityApi,
                                  UnityImplicitUsageHighlightingContributor implicitUsageHighlightingContributor)
            : base(unityApi)
        {
            myImplicitUsageHighlightingContributor = implicitUsageHighlightingContributor;
        }

        protected override void Analyze(IFieldDeclaration element, ElementProblemAnalyzerData data,
                                        IHighlightingConsumer consumer)
        {
            var field = element.DeclaredElement;
            if (Api.IsSerialisedField(field) || Api.IsInjectedField(field))
            {
                myImplicitUsageHighlightingContributor.AddUnityImplicitFieldUsage(consumer, element,
                    "This field is initialised by Unity", "Set by Unity");
            }
        }
    }
}