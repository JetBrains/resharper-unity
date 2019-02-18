using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.GutterMarks
{
    [ElementProblemAnalyzer(typeof(IClassLikeDeclaration), HighlightingTypes = new[]
    {
        typeof(UnityGutterMarkInfo),
#if RIDER
        typeof(JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights.UnityCodeInsightsHighlighting)
#endif
    })]
    public class UnityTypeDetector : UnityElementProblemAnalyzer<IClassLikeDeclaration>
    {
        private readonly UnityImplicitUsageHighlightingContributor myImplicitUsageHighlightingContributor;

        public UnityTypeDetector(UnityApi unityApi,
            UnityImplicitUsageHighlightingContributor implicitUsageHighlightingContributor)
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
                    myImplicitUsageHighlightingContributor.AddUnityImplicitClassUsage(consumer, element,
                        "Unity scripting component", "Scripting component");
                }
                else if (Api.IsUnityECSType(typeElement))
                {
                    myImplicitUsageHighlightingContributor.AddUnityImplicitClassUsage(consumer, element,
                        "Unity entity component system object", "Unity ECS");
                }
                else if (Api.IsSerializableType(typeElement))
                {
                    myImplicitUsageHighlightingContributor.AddUnityImplicitClassUsage(consumer, element,
                        "Unity custom serializable type", "Unity serializable");
                }
            }
        }
    }
}