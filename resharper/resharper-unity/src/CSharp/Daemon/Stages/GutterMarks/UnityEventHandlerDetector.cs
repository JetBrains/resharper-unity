using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.GutterMarks
{
    [ElementProblemAnalyzer(typeof(IMethodDeclaration), typeof(IPropertyDeclaration), HighlightingTypes = new[]
    {
        typeof(UnityGutterMarkInfo),
#if RIDER
        typeof(JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights.UnityCodeInsightsHighlighting)
#endif
    })]
    public class UnityEventHandlerDetector : UnityElementProblemAnalyzer<IDeclaration>
    {
        private readonly UnityEventHandlerReferenceCache myCache;
        private readonly UnityImplicitUsageHighlightingContributor myImplicitUsageHighlightingContributor;

        public UnityEventHandlerDetector([NotNull] UnityApi unityApi, UnityEventHandlerReferenceCache cache,
                                         UnityImplicitUsageHighlightingContributor implicitUsageHighlightingContributor)
            : base(unityApi)
        {
            myCache = cache;
            myImplicitUsageHighlightingContributor = implicitUsageHighlightingContributor;
        }

        protected override void Analyze(IDeclaration element, ElementProblemAnalyzerData data,
                                        IHighlightingConsumer consumer)
        {
            var declaredElement = element is IPropertyDeclaration
                ? ((IProperty) element.DeclaredElement)?.Setter
                : element.DeclaredElement as IMethod;
            if (declaredElement != null && myCache.IsEventHandler(declaredElement))
            {
                myImplicitUsageHighlightingContributor.AddUnityEventHandler(consumer, element, "Unity event handler", element is IMethodDeclaration ? "Event function" : "Implicit usage");
            }
        }
    }
}