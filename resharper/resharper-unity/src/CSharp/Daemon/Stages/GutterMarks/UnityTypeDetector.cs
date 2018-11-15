using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
#if RIDER
using JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights;
#endif
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.GutterMarks
{
    [ElementProblemAnalyzer(typeof(IClassLikeDeclaration), HighlightingTypes = new[]
    {
#if RIDER
            typeof(UnityCodeInsightsHighlighting)
#else
        typeof(UnityGutterMarkInfo),
#endif
    })]
    public class UnityTypeDetector : UnityElementProblemAnalyzer<IClassLikeDeclaration>
    {
        private readonly UnityImplicitUsageHighlightingContributor myImplicitUsageHighlightingContributor;

        public UnityTypeDetector(UnityApi unityApi, UnityImplicitUsageHighlightingContributor implicitUsageHighlightingContributor)
            : base(unityApi)
        {
            myImplicitUsageHighlightingContributor = implicitUsageHighlightingContributor;
        }

        protected override void Analyze(IClassLikeDeclaration element, ElementProblemAnalyzerData data,
            IHighlightingConsumer consumer)
        {
            var typeElement = element.DeclaredElement;
            if (typeElement != null)
            {
                if (Api.IsUnityType(typeElement))
                {
                    myImplicitUsageHighlightingContributor.AddUnityImplicitClassUsage(consumer, element, "Unity scripting component");
                }
                else if (Api.IsSerializableType(typeElement))
                {
                    myImplicitUsageHighlightingContributor.AddUnityImplicitClassUsage(consumer, element, "Unity custom serializable type");
                }
            }
        }
    }
}