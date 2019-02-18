using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.GutterMarks
{
    [ElementProblemAnalyzer(typeof(IConstructorDeclaration), HighlightingTypes = new[]
    {
        typeof(UnityGutterMarkInfo),
#if RIDER
        typeof(JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights.UnityCodeInsightsHighlighting)
#endif
    })]
    public class UnityInitialiseOnLoadCctorDetector : UnityElementProblemAnalyzer<IConstructorDeclaration>
    {
        private readonly UnityImplicitUsageHighlightingContributor myImplicitUsageHighlightingContributor;

        public UnityInitialiseOnLoadCctorDetector(UnityApi unityApi,
                                                  UnityImplicitUsageHighlightingContributor
                                                      implicitUsageHighlightingContributor)
            : base(unityApi)
        {
            myImplicitUsageHighlightingContributor = implicitUsageHighlightingContributor;
        }

        protected override void Analyze(IConstructorDeclaration element, ElementProblemAnalyzerData data,
                                        IHighlightingConsumer consumer)
        {
            if (!element.IsStatic)
                return;

            var containingType = element.GetContainingTypeDeclaration()?.DeclaredElement;
            if (containingType != null &&
                containingType.HasAttributeInstance(KnownTypes.InitializeOnLoadAttribute, false))
            {
                myImplicitUsageHighlightingContributor.AddInitializeOnLoadMethod(consumer, element,
                    "Called when Unity first launches the editor, the player, or recompiles scripts", "Unity implicit usage");
            }
        }
    }
}